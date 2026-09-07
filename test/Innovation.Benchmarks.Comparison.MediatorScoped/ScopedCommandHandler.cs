namespace Innovation.Benchmarks.Comparison.MediatorScoped
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    /// <summary>
    /// Handles ScopedCommand with no work, kept IO-free so the benchmark measures only dispatch
    /// overhead - mirrors MediatorLib.BlankCommandHandler.
    /// </summary>
    public class ScopedCommandHandler : ICommandHandler<ScopedCommand, ScopedCommandResult>
    {
        private static readonly ScopedCommandResult commandResult = new ScopedCommandResult();

        public ValueTask<ScopedCommandResult> Handle(ScopedCommand command, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(commandResult);
        }
    }
}
