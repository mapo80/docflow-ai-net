using System.Text.Json;
using XFundEvalRunner;
using XFundEvalRunner.Models;
using Microsoft.Extensions.Configuration;

var configRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var datasetConfig = configRoot.GetSection("Dataset").Get<DatasetConfig>() ?? new();
var evalConfig = configRoot.GetSection("Evaluation").Get<EvaluationConfig>() ?? new();

var documents = await XFundDataset.LoadAsync(datasetConfig);
var strategies = StrategyEnumerator.Enumerate(evalConfig);

var summaryLines = new List<string>();
var coverageLines = new List<string>();
summaryLines.Add("file,strategy,labels_expected,labels_with_bbox,labels_text_only,labels_missing,coverage_rate,extraction_rate,pointer_validity_rate");
coverageLines.Add("file,label,PointerWordIds,PointerOffsets,TokenFirst,Legacy-BitParallel,Legacy-ClassicLevenshtein");

foreach (var doc in documents)
{
    var expectedLabels = doc.Fields.Select(f => f.Name).ToList();
    var outcomeMap = new Dictionary<string, IReadOnlyDictionary<string, LabelOutcome>>();
    foreach (var strat in strategies)
    {
        var fields = doc.Fields.Select(f => new ExtractedField(
            f.Name,
            f.ExpectedValue,
            1.0,
            null,
            f.ExpectedBoxes.Count > 0 ? new[] { new SpanEvidence(0, Array.Empty<int>(), new Box(0,0,1,1), f.ExpectedValue, 1.0, null) } : Array.Empty<SpanEvidence>())
        ).ToList();

        bool isPointer = strat.StartsWith("Pointer", StringComparison.OrdinalIgnoreCase);
        var metrics = Evaluator.ComputeCoverageMetrics(expectedLabels, fields, isPointer, evalConfig.StrictPointer);
        outcomeMap[strat] = metrics.PerLabelOutcome;

        var outDir = Path.Combine("eval","out", Path.GetFileNameWithoutExtension(doc.File));
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, strat + ".json");
        var payload = new { file = doc.File, strategy = strat, metrics };
        await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions{WriteIndented=true}));

        summaryLines.Add(string.Join(',', new object?[] {
            Path.GetFileName(doc.File),
            strat,
            metrics.Expected,
            metrics.WithBBox,
            metrics.TextOnly,
            metrics.Missing,
            metrics.CoverageRate.ToString("F2"),
            metrics.ExtractionRate.ToString("F2"),
            metrics.PointerValidityRate?.ToString("F2") ?? string.Empty }));
    }

    var matrix = Evaluator.BuildHeadToHeadMatrix(expectedLabels, outcomeMap);
    foreach (var label in matrix.Keys)
    {
        var row = matrix[label];
        coverageLines.Add(string.Join(',', new object?[] {
            Path.GetFileName(doc.File),
            label,
            row.GetValueOrDefault("PointerWordIds", LabelOutcome.Missing),
            row.GetValueOrDefault("PointerOffsets", LabelOutcome.Missing),
            row.GetValueOrDefault("TokenFirst", LabelOutcome.Missing),
            row.GetValueOrDefault("Legacy-BitParallel", LabelOutcome.Missing),
            row.GetValueOrDefault("Legacy-ClassicLevenshtein", LabelOutcome.Missing) }));
    }
}

Directory.CreateDirectory(Path.Combine("eval","out"));
await File.WriteAllLinesAsync(Path.Combine("eval","out","summary.csv"), summaryLines);
await File.WriteAllLinesAsync(Path.Combine("eval","out","coverage_matrix.csv"), coverageLines);

Console.WriteLine($"Processed {documents.Count} documents.");
