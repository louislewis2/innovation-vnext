namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using MediatR;

    /// <summary>
    /// MediatR equivalent of InnovationLib.BlankQuery - an IO-free request/response pair.
    /// </summary>
    public class BlankQuery : IRequest<BlankQueryResult>
    {
    }

    public class BlankQueryResult
    {
    }
}
