namespace DocflowAi.Net.BBoxResolver;

/// <summary>Orchestrates resolution of fields to bounding boxes using multiple strategies.</summary>
public interface IResolverOrchestrator
{
    Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default);
}

