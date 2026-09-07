namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using global::Mediator;

    /// <summary>
    /// Same shape as BlankCommand, but dispatched with a validation + logging pipeline behavior
    /// registered - the "loaded" scenario. See LoadedCommandComparisonTests for the rationale.
    /// </summary>
    public class LoadedCommand : ICommand<LoadedCommandResult>
    {
    }

    public class LoadedCommandResult
    {
        public bool Success => true;
    }
}
