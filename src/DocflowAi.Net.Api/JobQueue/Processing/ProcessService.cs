using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace DocflowAi.Net.Api.JobQueue.Processing;

/// <summary>
/// Default implementation of <see cref="IProcessService"/>. Currently a placeholder
/// that simulates a processing pipeline by reading optional prompt and fields
/// and returning a dummy JSON payload. It is designed to be replaced by the real
/// pipeline in future steps.
/// </summary>
public class ProcessService : IProcessService
{
    private readonly Serilog.ILogger _logger = Log.ForContext<ProcessService>();

    public async Task<ProcessResult> ExecuteAsync(ProcessInput input, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var fileInfo = new FileInfo(input.InputPath);
            var hasPrompt = input.PromptPath != null && File.Exists(input.PromptPath);
            var hasFields = input.FieldsPath != null && File.Exists(input.FieldsPath);
            _logger.Information("ProcessStarted {JobId} {Bytes} {FileExt} {HasPrompt} {HasFields}",
                input.JobId, fileInfo.Length, fileInfo.Extension, hasPrompt, hasFields);

            string prompt = hasPrompt ? await File.ReadAllTextAsync(input.PromptPath!, ct) : string.Empty;
            string fields = hasFields ? await File.ReadAllTextAsync(input.FieldsPath!, ct) : "{}";
            // Simulate some processing - this should be replaced with real logic.
            var output = new
            {
                promptLength = prompt.Length,
                fieldsLength = fields.Length,
                processedAtUtc = DateTimeOffset.UtcNow
            };
            var json = JsonSerializer.Serialize(output);
            sw.Stop();
            _logger.Information("ProcessCompleted {JobId} {ElapsedMs}", input.JobId, sw.ElapsedMilliseconds);
            return new ProcessResult(true, json, null);
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("ProcessCancelled {JobId}", input.JobId);
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.Error(ex, "ProcessFailed {JobId} {ElapsedMs}", input.JobId, sw.ElapsedMilliseconds);
            return new ProcessResult(false, string.Empty, ex.Message);
        }
    }
}
