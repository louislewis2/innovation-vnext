namespace Innovation.Benchmarks.Comparison
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Dispatching;

    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Measures Command_Innovation_Loaded's cost when a new DI scope is created and a fresh
    /// IDispatcher is resolved on every call, instead of resolving IDispatcher once in GlobalSetup and
    /// reusing it (as LoadedCommandComparisonTests does). This mirrors how ASP.NET Core actually
    /// behaves in production: one new DI scope is created per HTTP request, and a Transient IDispatcher
    /// (see ServiceCollectionExtensions.cs) is constructed fresh within that scope - which is also when
    /// Dispatcher's own internal IServiceScope is created (see Dispatcher's constructor).
    ///
    /// This class exists to be directly, provably comparable to
    /// MediatorScopedLoadedPerRequestComparisonTests.Command_Mediator_Scoped_Loaded_PerRequest in the
    /// Innovation.Benchmarks.Comparison.MediatorScoped project, which applies the identical
    /// CreateScope-per-call pattern to Mediator's Scoped configuration. Neither
    /// LoadedCommandComparisonTests.Command_Innovation_Loaded nor
    /// MediatorScopedLoadedComparisonTests.Command_Mediator_Scoped_Loaded include this per-request
    /// setup cost - both resolve their entry point once in GlobalSetup - so those two numbers are not
    /// a full "cost of one request" comparison. These two per-request benchmarks are. See BENCHMARKS.md,
    /// "Per-request scope cost", for the full rationale and results.
    /// </summary>
    [MemoryDiagnoser]
    public class LoadedCommandPerRequestComparisonTests
    {
        private InnovationLoadedProvider innovationProvider;

        private static readonly InnovationLib.LoadedCommand innovationCommand = new InnovationLib.LoadedCommand();

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.innovationProvider = new InnovationLoadedProvider();
        }

        [Benchmark]
        public async ValueTask<ICommandResult> Command_Innovation_Loaded_PerRequest()
        {
            using var scope = this.innovationProvider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

            return await dispatcher.Command(command: innovationCommand);
        }
    }
}
