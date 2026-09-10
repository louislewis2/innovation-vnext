namespace Innovation.Benchmarks.PipelineTests
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;
    using Innovation.Api.vNext.Dispatching;

    using Innovation.ApiSample;

    /// <summary>
    /// Benchmarks Dispatcher.Query end-to-end using BlankQuery/BlankQueryResult, which have exactly one
    /// IO-free handler registered. Complements DispatcherCommandTests - the Query pipeline is shorter
    /// than Command's (no reactors/interceptors/validators), but still worth tracking on its own since it
    /// resolves a handler, checks IContextAware/ICorrelationAware, and reports to the audit store.
    /// </summary>
    [MemoryDiagnoser]
    public class DispatcherQueryTests : DependencyBuilderBase
    {
        #region Fields

        private IDispatcher dispatcher;
        private static readonly BlankQuery blankQuery = new BlankQuery();

        #endregion Fields

        #region Methods

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.dispatcher = this.GetRequiredService<IDispatcher>();
        }

        [Benchmark]
        public async ValueTask<BlankQueryResult> DispatchBlankQuery()
        {
            return await dispatcher.Query<BlankQuery, BlankQueryResult>(query: blankQuery, cancellationToken: System.Threading.CancellationToken.None);
        }

        #endregion Methods
    }
}
