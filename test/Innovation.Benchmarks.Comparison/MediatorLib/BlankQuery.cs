namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using global::Mediator;

    /// <summary>
    /// Mediator (martinothamar) equivalent of InnovationLib.BlankQuery - an IO-free query/response pair.
    /// </summary>
    public class BlankQuery : IQuery<BlankQueryResult>
    {
    }

    public class BlankQueryResult
    {
    }
}
