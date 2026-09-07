namespace Innovation.Benchmarks.Comparison.MediatorScoped
{
    using global::Mediator;

    /// <summary>
    /// Equivalent of MediatorLib.BlankCommand, compiled into an assembly configured with
    /// [assembly: MediatorOptions(ServiceLifetime = ServiceLifetime.Scoped)] instead of the default
    /// Singleton, so the source generator emits the non-caching request wrapper (see
    /// BENCHMARKS.md "Confirming what Scoped mode actually generates").
    /// </summary>
    public class ScopedCommand : ICommand<ScopedCommandResult>
    {
    }

    public class ScopedCommandResult
    {
        public bool Success => true;
    }
}
