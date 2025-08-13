using BBoxEvalRunner;
using BBoxEvalRunner.Models;
using DocflowAi.Net.BBoxResolver;
using FluentAssertions;
using Xunit;

namespace DocflowAi.Net.BBoxResolver.Tests;

public class BBoxEvalRunnerTests
{
    [Fact]
    public void CoverageMetrics_Computed_From_Evidence()
    {
        var expected = new[] { "ragione_sociale", "partita_iva", "indirizzo", "codice_fiscale" };
        var fields = new List<ExtractedField>
        {
            new("ragione_sociale", "ACME", 1.0, new[]{new SpanEvidence(0, new[]{1}, new Box(0,0,1,1),"ACME",1.0,null)}),
            new("partita_iva", "123", 1.0, new[]{new SpanEvidence(0, new[]{2}, new Box(0,0,1,1),"123",1.0,null)}),
            new("indirizzo", "Via Roma", 1.0, new[]{new SpanEvidence(0, new[]{3}, new Box(0,0,1,1),"Via Roma",1.0,null)})
        };

        var metrics = Evaluator.ComputeCoverageMetrics(expected, fields, pointerStrategy:false, strictPointer:false);
        metrics.LabelsExpected.Should().Be(4);
        metrics.LabelsWithBBox.Should().Be(3);
        metrics.LabelsMissing.Should().Be(1);
        metrics.LabelCoverageRate.Should().Be(0.75);
        metrics.LabelExtractionRate.Should().Be(0.75);
    }

    [Fact]
    public void HeadToHeadMatrix_Builds_Correct_Cells()
    {
        var labels = new[] { "a", "b" };
        var stratA = new Dictionary<string, LabelOutcome>{{"a", LabelOutcome.WithBBox},{"b", LabelOutcome.TextOnly}};
        var stratB = new Dictionary<string, LabelOutcome>{{"a", LabelOutcome.Missing},{"b", LabelOutcome.WithBBox}};
        var matrix = Evaluator.BuildHeadToHeadMatrix(labels, new Dictionary<string, IReadOnlyDictionary<string, LabelOutcome>>
        {
            {"PointerWordIds", stratA},
            {"TokenFirst", stratB}
        });

        matrix["a"]["PointerWordIds"].Should().Be(LabelOutcome.WithBBox);
        matrix["a"]["TokenFirst"].Should().Be(LabelOutcome.Missing);
        matrix["b"]["PointerWordIds"].Should().Be(LabelOutcome.TextOnly);
        matrix["b"]["TokenFirst"].Should().Be(LabelOutcome.WithBBox);
    }

    [Fact]
    public void Legacy_Runs_Both_Algos_Separately()
    {
        var cfg = new EvaluationConfig
        {
            RunLegacy = true,
            LegacyAlgos = ["BitParallel","ClassicLevenshtein"]
        };
        var strategies = StrategyEnumerator.Enumerate(cfg).ToList();
        strategies.Should().Contain(("Legacy-BitParallel","BitParallel"));
        strategies.Should().Contain(("Legacy-ClassicLevenshtein","ClassicLevenshtein"));
    }

    [Fact]
    public void Manifest_Overrides_Global_Fields()
    {
        using var temp = new TempDir();
        var manifestPath = Path.Combine(temp.Path, "manifest.json");
        File.WriteAllText(manifestPath, "[ { \"file\": \"sample2.png\", \"fields\": [\"ragione_sociale\", \"partita_iva\"] } ]");
        var cfg = new DatasetConfig
        {
            Fields = ["ragione_sociale","partita_iva","indirizzo","codice_fiscale"],
            Manifest = manifestPath
        };
        var fields = DatasetLoader.GetExpectedLabels("sample2.png", cfg);
        fields.Should().HaveCount(2);
    }

    [Fact]
    public void Timing_Aggregation_Median_P95()
    {
        var (median, p95) = TimingAggregator.Aggregate(new List<double> { 100, 200, 300 });
        median.Should().Be(200);
        p95.Should().Be(300);
    }

    [Fact]
    public void PointerValidity_Rate_Populated()
    {
        var expected = new[] { "field" };
        var validField = new ExtractedField("field", "v", 1.0,
            new[]{new SpanEvidence(0, new[]{1}, new Box(0,0,1,1),"v",1.0,null)},
            new Pointer(PointerMode.WordIds, ["1"], null, null));
        var invalidField = new ExtractedField("field", "v", 1.0,
            new[]{new SpanEvidence(0, new[]{1}, new Box(0,0,1,1),"v",1.0,null)},
            new Pointer(PointerMode.WordIds, Array.Empty<string>(), null, null));

        var ok = Evaluator.ComputeCoverageMetrics(expected, [validField], pointerStrategy:true, strictPointer:true);
        ok.LabelPointerValidityRate.Should().Be(1);
        var ko = Evaluator.ComputeCoverageMetrics(expected, [invalidField], pointerStrategy:true, strictPointer:true);
        ko.LabelPointerValidityRate.Should().Be(0);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString());
        public TempDir() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
