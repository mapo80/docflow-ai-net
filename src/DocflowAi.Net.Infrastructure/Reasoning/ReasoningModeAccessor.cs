using DocflowAi.Net.Application.Abstractions;
namespace DocflowAi.Net.Infrastructure.Reasoning;
public sealed class ReasoningModeAccessor : IReasoningModeAccessor { public ReasoningMode Mode { get; set; } = ReasoningMode.Auto; }
