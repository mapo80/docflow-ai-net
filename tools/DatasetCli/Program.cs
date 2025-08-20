using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.IO;

var switchMap = new Dictionary<string,string>
{
    {"--dataset", "Dataset"},
    {"--output", "Output"},
    {"--apiKey", "ApiKey"},
    {"--baseUrl", "BaseUrl"},
    {"--template", "Template"},
    {"--model", "Model"}
};

var config = new ConfigurationBuilder()
    .AddCommandLine(args, switchMap)
    .Build();

var dataset = config["Dataset"] ?? throw new ArgumentException("dataset required");
var output = config["Output"] ?? "results.json";
var apiKey = config["ApiKey"] ?? "dev-secret-key-change-me";
var baseUrl = config["BaseUrl"] ?? "http://localhost:8080";
var template = config["Template"] ?? new DirectoryInfo(dataset).Name;
var model = config["Model"] ?? "default";

var files = Directory.EnumerateFiles(dataset)
    .Where(f => {
        var ext = Path.GetExtension(f).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".pdf";
    })
    .OrderBy(f => f)
    .ToList();

var http = new HttpClient();
http.DefaultRequestHeaders.Add("X-API-Key", apiKey);

var items = new List<object>();
var mdTimes = new List<double>();
var llmTimes = new List<double>();
var totalTimes = new List<double>();

foreach (var file in files)
{
    var bytes = await File.ReadAllBytesAsync(file);
    var payload = new
    {
        fileBase64 = Convert.ToBase64String(bytes),
        fileName = Path.GetFileName(file),
        model,
        templateToken = template
    };
    var submitResp = await http.PostAsJsonAsync($"{baseUrl}/api/v1/jobs?mode=immediate", payload);
    submitResp.EnsureSuccessStatusCode();
    var submit = await submitResp.Content.ReadFromJsonAsync<SubmitResponse>();
    if (submit == null) throw new InvalidOperationException("submit failed");

    JobDetail? detail;
    do
    {
        await Task.Delay(500);
        detail = await http.GetFromJsonAsync<JobDetail>($"{baseUrl}{submit.status_url}");
    } while (detail != null && (detail.Status == "Queued" || detail.Status == "Running"));

    if (detail?.Status == "Succeeded" && detail.Paths.Output != null)
    {
        var json = await http.GetStringAsync($"{baseUrl}{detail.Paths.Output}");
        var doc = JsonDocument.Parse(json);
        var metrics = doc.RootElement.GetProperty("metrics");
        double md = metrics.GetProperty("markdown_ms").GetDouble();
        double llm = metrics.GetProperty("llm_ms").GetDouble();
        double total = metrics.GetProperty("total_ms").GetDouble();
        mdTimes.Add(md);
        llmTimes.Add(llm);
        totalTimes.Add(total);
        var fieldMap = doc.RootElement.GetProperty("fields").EnumerateArray().ToDictionary(
            f => f.GetProperty("key").GetString()!,
            f => f.GetProperty("value").GetString() ?? string.Empty
        );
        items.Add(new { file = Path.GetFileName(file), fields = fieldMap, metrics = new { markdown_ms = md, llm_ms = llm, total_ms = total } });
    }
    else
    {
        items.Add(new { file = Path.GetFileName(file), error = detail?.ErrorMessage ?? "unknown" });
    }
}

var summary = new
{
    items,
    average = new
    {
        markdown_ms = mdTimes.DefaultIfEmpty(0).Average(),
        llm_ms = llmTimes.DefaultIfEmpty(0).Average(),
        total_ms = totalTimes.DefaultIfEmpty(0).Average()
    }
};

var options = new JsonSerializerOptions { WriteIndented = true };
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(summary, options));
Console.WriteLine(JsonSerializer.Serialize(summary, options));

record SubmitResponse(Guid job_id, string status_url, string? dashboard_url);

record MetricsInfo(DateTimeOffset? StartedAt, DateTimeOffset? EndedAt, long? DurationMs);
record PathInfo(string Dir, string? Input, string? Output, string? Error, string? Markdown);
record JobDetail(Guid Id, string Status, string? DerivedStatus, int Progress, int Attempts, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, MetricsInfo Metrics, PathInfo Paths, string? ErrorMessage, string Model, string TemplateToken);
