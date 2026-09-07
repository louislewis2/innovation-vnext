namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using MediatR;

    /// <summary>
    /// Handles BlankCommand with no work, kept IO-free so the benchmark measures only dispatch overhead.
    /// </summary>
    public class BlankCommandHandler : IRequestHandler<BlankCommand, BlankCommandResult>
    {
        private static readonly BlankCommandResult commandResult = new BlankCommandResult();

        public Task<BlankCommandResult> Handle(BlankCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(commandResult);
        }
    }
}
