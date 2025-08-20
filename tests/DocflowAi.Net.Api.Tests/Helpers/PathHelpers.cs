namespace DocflowAi.Net.Api.Tests.Helpers;

public static class PathHelpers
{
    public static string JobDir(string dataRoot, Guid jobId)
        => Path.Combine(dataRoot, jobId.ToString("N"));

    public static string OutputPath(string dataRoot, Guid jobId)
        => Path.Combine(JobDir(dataRoot, jobId), "output.json");

    public static string ErrorPath(string dataRoot, Guid jobId)
        => Path.Combine(JobDir(dataRoot, jobId), "error.txt");

    public static string MarkdownPath(string dataRoot, Guid jobId)
        => Path.Combine(JobDir(dataRoot, jobId), "markdown.md");
}
