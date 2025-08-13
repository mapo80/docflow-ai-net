namespace DocflowAi.Net.BBoxResolver;

/// <summary>Resolves fields to bounding boxes.</summary>
public interface IBBoxResolver
{
    /// <summary>Resolve fields against a document index.</summary>
    Task<IReadOnlyList<BBoxResolveResult>> ResolveAsync(DocumentIndex index, IReadOnlyList<ExtractedField> fields, CancellationToken ct = default);
}
