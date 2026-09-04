namespace Innovation.SampleApi.Consumer.Handlers.InterceptorTest
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Interceptors;

    using Innovation.ApiSample;

    /// <summary>
    /// A no-op interceptor for InterceptorTestCommand. Unlike InsertCustomerInterceptor (which performs a
    /// real GitHub API call), this exists purely so the Command Interceptor stage can be benchmarked
    /// without introducing any IO - we want to measure the dispatcher's interceptor-resolution/invocation
    /// overhead, not network latency.
    /// </summary>
    public class InterceptorTestCommandInterceptor : ICommandInterceptor<InterceptorTestCommand>
    {
        #region Methods

        public ValueTask Intercept(InterceptorTestCommand command)
        {
            return ValueTask.CompletedTask;
        }

        #endregion Methods
    }
}
