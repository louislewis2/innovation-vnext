namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Commanding;

    /// <summary>
    /// Handles LoadedCommand with no work, kept IO-free so the benchmark measures only pipeline overhead.
    /// </summary>
    public class LoadedCommandHandler : ICommandHandler<LoadedCommand>
    {
        private static readonly ICommandResult commandResult = new CommandResult();

        public ValueTask<ICommandResult> Handle(LoadedCommand command)
        {
            return ValueTask.FromResult(commandResult);
        }

        private sealed class CommandResult : ICommandResult
        {
            public bool Success => true;
        }
    }
}
