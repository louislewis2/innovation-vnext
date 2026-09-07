namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    /// <summary>
    /// Handles LoadedCommand with no work, kept IO-free so the benchmark measures only pipeline overhead.
    /// </summary>
    public class LoadedCommandHandler : ICommandHandler<LoadedCommand, LoadedCommandResult>
    {
        private static readonly LoadedCommandResult commandResult = new LoadedCommandResult();

        public ValueTask<LoadedCommandResult> Handle(LoadedCommand command, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(commandResult);
        }
    }
}
