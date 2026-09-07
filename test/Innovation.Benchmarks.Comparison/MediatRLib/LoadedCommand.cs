namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using MediatR;

    /// <summary>
    /// Same shape as BlankCommand, but dispatched with a validation + logging pipeline behavior
    /// registered - the "loaded" scenario. See LoadedCommandComparisonTests for the rationale.
    /// </summary>
    public class LoadedCommand : IRequest<LoadedCommandResult>
    {
    }

    public class LoadedCommandResult
    {
        public bool Success => true;
    }
}
