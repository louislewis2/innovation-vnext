namespace Innovation.Benchmarks.PipelineTests
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;
    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Dispatching;

    using Innovation.ApiSample.Vendors.Commands;
    using Innovation.ApiSample.Vendors.Criteria;
    using Innovation.ApiSample.Shared.Criteria;

    /// <summary>
    /// Benchmarks InnovationOptions.AggregateValidationErrors: fail-fast (default - dispatch stops at the
    /// first validator that reports an error) vs aggregate (every registered validator runs and all errors
    /// are merged into a single result). Uses an InsertVendorCommand that is deliberately invalid against
    /// BOTH DataAnnotations and the custom InsertVendorAddressValidator, so aggregation actually has two
    /// error sets to merge - a command failing only one validator wouldn't show the difference.
    /// </summary>
    [MemoryDiagnoser]
    public class ValidationAggregationComparisonTests
    {
        #region Fields

        private IDispatcher failFastDispatcher;
        private IDispatcher aggregateDispatcher;

        private static readonly InsertVendorCommand invalidVendorCommand = new InsertVendorCommand(
            vendorCriteria: new VendorCriteria(
                name: "a",
                userName: "short",
                addressCriteria: new AddressCriteria(line1: "111 Street", code: "00000")));

        #endregion Fields

        #region Methods

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.failFastDispatcher = new FailFastProvider().GetRequiredService<IDispatcher>();
            this.aggregateDispatcher = new AggregateProvider().GetRequiredService<IDispatcher>();
        }

        [Benchmark(Baseline = true)]
        public async ValueTask<ICommandResult> FailFast() => await failFastDispatcher.Command(command: invalidVendorCommand, cancellationToken: System.Threading.CancellationToken.None);

        [Benchmark]
        public async ValueTask<ICommandResult> Aggregate() => await aggregateDispatcher.Command(command: invalidVendorCommand, cancellationToken: System.Threading.CancellationToken.None);

        #endregion Methods

        #region Providers

        private sealed class FailFastProvider : DependencyBuilderBase
        {
            public FailFastProvider() : base(includeAuditStore: true, configureOptions: options => options.AggregateValidationErrors = false)
            {
            }
        }

        private sealed class AggregateProvider : DependencyBuilderBase
        {
            public AggregateProvider() : base(includeAuditStore: true, configureOptions: options => options.AggregateValidationErrors = true)
            {
            }
        }

        #endregion Providers
    }
}
