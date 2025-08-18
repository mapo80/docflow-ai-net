namespace DocflowAi.Net.Application.Abstractions;

/// <summary>
/// Generic paged result.
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total);
