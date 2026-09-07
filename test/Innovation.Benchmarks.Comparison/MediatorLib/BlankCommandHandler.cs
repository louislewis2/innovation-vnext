namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    /// <summary>
    /// Handles BlankCommand with no work, kept IO-free so the benchmark measures only dispatch overhead.
    /// </summary>
    public class BlankCommandHandler : ICommandHandler<BlankCommand, BlankCommandResult>
    {
        private static readonly BlankCommandResult commandResult = new BlankCommandResult();

        public ValueTask<BlankCommandResult> Handle(BlankCommand command, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(commandResult);
        }
    }
}
