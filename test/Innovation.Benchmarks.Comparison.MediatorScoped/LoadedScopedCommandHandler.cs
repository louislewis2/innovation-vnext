namespace Innovation.Benchmarks.Comparison.MediatorScoped
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    /// <summary>
    /// Handles LoadedScopedCommand with no work, kept IO-free so the benchmark measures only pipeline
    /// overhead - mirrors MediatorLib.LoadedCommandHandler.
    /// </summary>
    public class LoadedScopedCommandHandler : ICommandHandler<LoadedScopedCommand, LoadedScopedCommandResult>
    {
        private static readonly LoadedScopedCommandResult commandResult = new LoadedScopedCommandResult();

        public ValueTask<LoadedScopedCommandResult> Handle(LoadedScopedCommand command, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(commandResult);
        }
    }
}
