namespace DocflowAi.Net.Infrastructure.Markitdown;
public sealed class MarkitdownException: Exception { public System.Net.HttpStatusCode StatusCode { get; } public MarkitdownException(System.Net.HttpStatusCode statusCode,string message,Exception? inner=null): base(message,inner){ StatusCode=statusCode; } }
