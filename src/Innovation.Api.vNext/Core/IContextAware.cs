namespace Innovation.Api.vNext.Core
{
    using System.Diagnostics.CodeAnalysis;

    using Dispatching;

    public interface IContextAware
    {
        void SetContext([DisallowNull] IDispatcherContext dispatcherContext);
    }
}
