namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using Innovation.Api.vNext.Querying;

    /// <summary>
    /// Mirrors Innovation.ApiSample.BlankQuery - an IO-free query used only to exercise Query dispatch
    /// cost in the cross-library comparison.
    /// </summary>
    public class BlankQuery : IQuery
    {
        public string EventName => nameof(BlankQuery);
    }
}
