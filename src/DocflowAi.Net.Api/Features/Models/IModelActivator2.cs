
namespace DocflowAi.Net.Api.Features.Models;

public interface IModelActivator2
{
    Task ActivateAsync(ModelActivationPayload payload, CancellationToken ct = default);
}

public sealed class DelegateModelActivator2 : IModelActivator2
{
    private readonly Func<ModelActivationPayload, CancellationToken, Task> _fn;
    public DelegateModelActivator2(Func<ModelActivationPayload, CancellationToken, Task> fn) => _fn = fn;
    public Task ActivateAsync(ModelActivationPayload payload, CancellationToken ct = default) => _fn(payload, ct);
}
