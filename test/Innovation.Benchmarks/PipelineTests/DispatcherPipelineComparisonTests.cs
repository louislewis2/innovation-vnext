namespace Innovation.Benchmarks.PipelineTests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    using MiniValidation;
    using BenchmarkDotNet.Attributes;
    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Dispatching;

    using Innovation.ApiSample;
    using Innovation.ApiSample.Vendors.Commands;
    using Innovation.ApiSample.Vendors.Criteria;
    using Innovation.ApiSample.Shared.Criteria;

    /// <summary>
    /// Benchmarks the Command hot path cost across the pipeline shapes that get turned on incrementally
    /// as framework features are opted into: nothing registered, reactor + result-reactor only,
    /// interceptor only, and DataAnnotations + custom-validator only. This is what a consumer's real
    /// pipeline shape drifts towards as they add cross-cutting behaviors, so it's useful to keep an eye
    /// on the relative/incremental cost of each stage rather than just the fully-blank baseline.
    ///
    /// Blank uses DependencyBuilderBase's default constructor (no IAuditStore registered), so it is the
    /// framework's true zero-registration floor - see AuditStoreComparisonTests for that cost in isolation.
    ///
    /// All commands/handlers used here are IO-free by design - see InterceptorTestCommand's remarks for
    /// why InsertCustomerCommand (whose interceptor performs a real GitHub API call) is deliberately not
    /// used.
    /// </summary>
    [MemoryDiagnoser]
    public class DispatcherPipelineComparisonTests : DependencyBuilderBase
    {
        private IDispatcher dispatcher;
        private IServiceProvider serviceProvider;
        private static readonly BlankCommand blankCommand = new BlankCommand();
        private static readonly ReactorTestCommand reactorCommand = new ReactorTestCommand();
        private static readonly InterceptorTestCommand interceptorCommand = new InterceptorTestCommand();
        private static readonly InsertVendorCommand vendorCommand = new InsertVendorCommand(
            vendorCriteria: new VendorCriteria(
                name: "Innovation1",
                userName: "somecrazynamethatexists",
                addressCriteria: new AddressCriteria(line1: "1 Street", code: "00000")));

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.dispatcher = this.GetRequiredService<IDispatcher>();
            this.serviceProvider = this.GetRequiredService<IServiceProvider>();

            // Warm up MiniValidation's type cache so results are comparable across scenarios.
            MiniValidator.TryValidate(blankCommand, this.serviceProvider, out _);
            MiniValidator.TryValidate(interceptorCommand, this.serviceProvider, out _);
            MiniValidator.TryValidate(vendorCommand, this.serviceProvider, out _);
        }

        [Benchmark(Baseline = true)]
        public async ValueTask<ICommandResult> Blank() => await dispatcher.Command(command: blankCommand);

        [Benchmark]
        public async ValueTask<ICommandResult> ReactorAndResultReactor() => await dispatcher.Command(command: reactorCommand);

        [Benchmark]
        public async ValueTask<ICommandResult> Interceptor() => await dispatcher.Command(command: interceptorCommand);

        [Benchmark]
        public async ValueTask<ICommandResult> DataAnnotationsAndCustomValidator() => await dispatcher.Command(command: vendorCommand);
    }
}
