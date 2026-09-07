namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using Innovation.Api.vNext.Querying;

    /// <summary>
    /// The result returned for BlankQuery. Carries no data, exists purely so Query dispatch can be
    /// benchmarked without any IO or business logic.
    /// </summary>
    public class BlankQueryResult : IQueryResult
    {
    }
}
