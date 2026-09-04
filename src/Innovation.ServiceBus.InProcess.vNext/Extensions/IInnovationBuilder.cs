namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Fluent extensibility seam returned by <c>AddInnovationvNext()</c>, used to override any of the
    /// library's default component registrations (e.g. the audit store, or the reactor work queue)
    /// after the library's own defaults have already been registered.
    /// </summary>
    public interface IInnovationBuilder
    {
        IServiceCollection Services { get; }
    }
}
