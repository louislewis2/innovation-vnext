namespace Innovation.ApiSample
{
    using Innovation.Api.vNext.Querying;

    /// <summary>
    /// The result returned for BlankQuery. Carries no data - it exists purely so the Query dispatch
    /// pipeline can be benchmarked without any IO or business logic.
    /// </summary>
    public class BlankQueryResult : IQueryResult
    {
    }
}
