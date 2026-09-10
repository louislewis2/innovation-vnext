namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Commanding;

    /// <summary>
    /// Handles BlankCommand with no work, kept IO-free so the benchmark measures only dispatch overhead.
    /// </summary>
    public class BlankCommandHandler : ICommandHandler<BlankCommand>
    {
        private static readonly ICommandResult commandResult = new CommandResult();

        public ValueTask<ICommandResult> Handle(BlankCommand command, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(commandResult);
        }

        private sealed class CommandResult : ICommandResult
        {
            public bool Success => true;
        }
    }
}
