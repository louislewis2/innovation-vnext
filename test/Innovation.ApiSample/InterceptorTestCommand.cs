namespace Innovation.ApiSample
{
    using Innovation.Api.vNext.Commanding;

    /// <summary>
    /// A dedicated command used only to exercise the Command Interceptor stage of the dispatch pipeline
    /// in isolation, so interceptor-pipeline benchmarks don't depend on InsertCustomerCommand - whose
    /// registered interceptor (InsertCustomerInterceptor) performs a real network call and is therefore
    /// unsuitable for benchmarking IO-free framework overhead.
    /// </summary>
    public class InterceptorTestCommand : ICommand
    {
        public string EventName => nameof(InterceptorTestCommand);
    }
}
