namespace Innovation.Benchmarks.Comparison.MediatorScoped
{
    using global::Mediator;

    /// <summary>
    /// Same shape as ScopedCommand, but dispatched with a validation + logging pipeline behavior
    /// registered under ServiceLifetime.Scoped - mirrors MediatorLib.LoadedCommand, so
    /// Command_Mediator_Scoped_Loaded is comparable to Command_Mediator_Loaded (Singleton, from the
    /// main comparison project) rather than only to the behavior-free Command_Mediator_Scoped.
    /// </summary>
    public class LoadedScopedCommand : ICommand<LoadedScopedCommandResult>
    {
    }

    public class LoadedScopedCommandResult
    {
        public bool Success => true;
    }
}
