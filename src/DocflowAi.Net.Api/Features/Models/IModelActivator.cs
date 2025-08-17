namespace DocflowAi.Net.Api.Features.Models;

/// <summary>
/// Abstraction to hot-activate a model file at runtime.
/// Implement this to reload your inference runtime (e.g., LLamaSharp) with the given GGUF path.
/// </summary>
public interface IModelActivator
{
    Task ActivateAsync(string localPath, CancellationToken ct = default);
}

/// <summary>
/// Convenience adapter that lets you pass a delegate instead of a concrete type.
/// </summary>
public sealed class DelegateModelActivator : IModelActivator
{
    private readonly Func<string, CancellationToken, Task> _fn;
    public DelegateModelActivator(Func<string, CancellationToken, Task> fn) => _fn = fn;
    public Task ActivateAsync(string localPath, CancellationToken ct = default) => _fn(localPath, ct);
}
