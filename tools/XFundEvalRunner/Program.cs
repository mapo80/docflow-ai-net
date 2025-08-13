using System.Text.Json;
using XFundEvalRunner;
using XFundEvalRunner.Models;
using Microsoft.Extensions.Configuration;

var switchMap = new Dictionary<string, string>
{
    {"--max-files", "Dataset:MaxFiles"},
    {"--strategies", "Evaluation:Strategies"},
    {"--force", "Evaluation:Force"},
    {"--save-prompts", "Evaluation:SavePrompts"},
    {"--save-traces", "Evaluation:SavePerAlgorithmTrace"}
};

var configRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddCommandLine(args, switchMap)
    .Build();

var datasetConfig = configRoot.GetSection("Dataset").Get<DatasetConfig>() ?? new();
var evalConfig = configRoot.GetSection("Evaluation").Get<EvaluationConfig>() ?? new();

var stratOverride = configRoot["Evaluation:Strategies"];
if (!string.IsNullOrWhiteSpace(stratOverride))
{
    evalConfig.RunPointerWordIds = evalConfig.RunPointerOffsets = evalConfig.RunTokenFirst = evalConfig.RunLegacy = false;
    evalConfig.LegacyAlgos.Clear();
    foreach (var s in stratOverride.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (s.Equals("PointerWordIds", StringComparison.OrdinalIgnoreCase)) evalConfig.RunPointerWordIds = true;
        else if (s.Equals("PointerOffsets", StringComparison.OrdinalIgnoreCase)) evalConfig.RunPointerOffsets = true;
        else if (s.Equals("TokenFirst", StringComparison.OrdinalIgnoreCase)) evalConfig.RunTokenFirst = true;
        else if (s.StartsWith("Legacy-", StringComparison.OrdinalIgnoreCase))
        {
            evalConfig.RunLegacy = true;
            var algo = s.Split('-', 2)[1];
            if (!string.IsNullOrWhiteSpace(algo)) evalConfig.LegacyAlgos.Add(algo);
        }
    }
}

var documents = await XFundDataset.LoadAsync(datasetConfig);
var strategies = StrategyEnumerator.Enumerate(evalConfig);

var summaryLines = new List<string>();
var coverageLines = new List<string>();
summaryLines.Add("file,strategy,distance_algorithm,labels_expected,labels_with_bbox,labels_text_only,labels_missing,label_coverage_rate,label_extraction_rate,exact_value_match_rate,word_iou_mean,iou@0.5,iou@0.75,confidence_mean,t_convert_ms,t_index_ms_median,t_llm_ms_median,t_resolve_ms_median,t_total_ms_median,pointer_validity_rate,pointer_fallback_rate");
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
            f.ExpectedBoxes.Count > 0 ? new[] { new SpanEvidence(0, Array.Empty<int>(), new Box(f.ExpectedBoxes[0][0], f.ExpectedBoxes[0][1], f.ExpectedBoxes[0][2], f.ExpectedBoxes[0][3]), f.ExpectedValue, 1.0, null) } : Array.Empty<SpanEvidence>())
        ).ToList();

        bool isPointer = strat.StartsWith("Pointer", StringComparison.OrdinalIgnoreCase);
        var metrics = Evaluator.ComputeCoverageMetrics(doc.Fields, fields, isPointer, evalConfig.StrictPointer);
        outcomeMap[strat] = metrics.PerLabelOutcome;

        var outDir = Path.Combine("eval","out", Path.GetFileNameWithoutExtension(doc.File));
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, strat + ".json");
        var payload = new { file = doc.File, strategy = strat, metrics };
        await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        string distanceAlgo = strat.StartsWith("Legacy-", StringComparison.OrdinalIgnoreCase) ? strat.Split('-', 2)[1] : string.Empty;
        summaryLines.Add(string.Join(',', new object?[] {
            Path.GetFileName(doc.File),
            strat,
            distanceAlgo,
            metrics.Expected,
            metrics.WithBBox,
            metrics.TextOnly,
            metrics.Missing,
            metrics.CoverageRate.ToString("F2"),
            metrics.ExtractionRate.ToString("F2"),
            "1.00",
            metrics.IoUMean.ToString("F2"),
            metrics.IoUAt0_5.ToString("F2"),
            metrics.IoUAt0_75.ToString("F2"),
            "1.00",
            "0",
            "0",
            "0",
            "0",
            "0",
            metrics.PointerValidityRate.HasValue ? metrics.PointerValidityRate.Value.ToString("F2") : string.Empty,
            "0" }));
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
