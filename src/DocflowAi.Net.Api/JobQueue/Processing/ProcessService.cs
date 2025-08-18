using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace DocflowAi.Net.Api.JobQueue.Processing;

/// <summary>
/// Default implementation of <see cref="IProcessService"/>. Currently a placeholder
/// that simulates a processing pipeline by logging model and template information
/// and returning a dummy JSON payload. It is designed to be replaced by the real
/// pipeline in future steps.
/// </summary>
public class ProcessService : IProcessService
{
    private readonly IModelDispatchService _dispatcher;
    private readonly Serilog.ILogger _logger = Log.ForContext<ProcessService>();

    public ProcessService(IModelDispatchService dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<ProcessResult> ExecuteAsync(ProcessInput input, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var fileInfo = new FileInfo(input.InputPath);
            _logger.Information(
                "ProcessStarted {JobId} {Bytes} {FileExt} {Template} {Model}",
                input.JobId,
                fileInfo.Length,
                fileInfo.Extension,
                input.TemplateToken,
                input.Model);

            var payload = JsonSerializer.Serialize(new { template = input.TemplateToken });
            var json = await _dispatcher.InvokeAsync(input.Model, payload, ct);
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
