namespace Innovation.Api.vNext.Dispatching
{
    using System.Diagnostics.CodeAnalysis;

    public interface IDispatcherContext
    {
        void SetCorrelationId([DisallowNull] string correlationId);
    }
}
