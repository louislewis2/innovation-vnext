namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using MediatR;

    /// <summary>
    /// Handles LoadedCommand with no work, kept IO-free so the benchmark measures only pipeline overhead.
    /// </summary>
    public class LoadedCommandHandler : IRequestHandler<LoadedCommand, LoadedCommandResult>
    {
        private static readonly LoadedCommandResult commandResult = new LoadedCommandResult();

        public Task<LoadedCommandResult> Handle(LoadedCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(commandResult);
        }
    }
}
