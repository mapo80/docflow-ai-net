namespace DocflowAi.Net.Application.Abstractions;
public enum ReasoningMode { Auto, Think, NoThink }
public interface IReasoningModeAccessor { ReasoningMode Mode { get; set; } }
