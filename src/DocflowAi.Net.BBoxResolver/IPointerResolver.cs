namespace DocflowAi.Net.BBoxResolver;

/// <summary>Resolves fields using pointer information.</summary>
public interface IPointerResolver
{
    /// <summary>Attempt to resolve a single field using pointers.</summary>
    bool TryResolve(DocumentIndex index, ExtractedField field, out BBoxResolveResult result);
}

