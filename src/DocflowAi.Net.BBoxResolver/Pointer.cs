namespace DocflowAi.Net.BBoxResolver;

/// <summary>Pointer information returned by the LLM.</summary>
/// <param name="Mode">Pointer mode.</param>
/// <param name="WordIds">Word identifiers when <see cref="Mode"/> is <see cref="PointerMode.WordIds"/>.</param>
/// <param name="Start">Start offset when <see cref="Mode"/> is <see cref="PointerMode.Offsets"/>.</param>
/// <param name="End">End offset when <see cref="Mode"/> is <see cref="PointerMode.Offsets"/>.</param>
public sealed record Pointer(PointerMode Mode, string[]? WordIds, int? Start, int? End);

