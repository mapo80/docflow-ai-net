namespace DocflowAi.Net.Application.Abstractions;
public interface IMarkitdownClient { Task<string> ToMarkdownAsync(Stream imageStream, string fileName, CancellationToken ct); }
