namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using Innovation.Api.vNext.Commanding;

    /// <summary>
    /// Same shape as BlankCommand, but used with validation and an audit store enabled - this is the
    /// "loaded" scenario, exercising the same cross-cutting concerns (validate, then log) that a
    /// MediatR/Mediator pipeline behavior equivalent would provide, so the comparison is framework-vs-
    /// framework rather than framework-vs-micro. See LoadedCommandComparisonTests for the rationale.
    /// </summary>
    public class LoadedCommand : ICommand
    {
        public string EventName => nameof(LoadedCommand);
    }
}
