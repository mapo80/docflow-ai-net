using DocflowAi.Net.Domain.Extraction; using Microsoft.AspNetCore.Http;
namespace DocflowAi.Net.Application.Abstractions;
public interface IProcessingOrchestrator { Task<DocumentAnalysisResult> ProcessAsync(IFormFile file, CancellationToken ct); }
