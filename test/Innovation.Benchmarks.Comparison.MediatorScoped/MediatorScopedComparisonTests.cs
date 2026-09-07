namespace Innovation.Benchmarks.Comparison.MediatorScoped
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;

    using global::Mediator;

    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Measures Mediator (martinothamar) dispatching a single, IO-free command under
    /// [assembly: MediatorOptions(ServiceLifetime = ServiceLifetime.Scoped)] (see AssemblyInfo.cs),
    /// as opposed to its default Singleton lifetime (measured in
    /// Innovation.Benchmarks.Comparison.CommandComparisonTests.Command_Mediator).
    ///
    /// This lives in its own console project, rather than alongside the Singleton comparison, because
    /// Mediator's ServiceLifetime is a compile-time, assembly-wide source generator setting - an
    /// assembly can only be compiled for one lifetime at a time (see BENCHMARKS.md, "Confirming what
    /// Scoped mode actually generates"). There is deliberately no BenchmarkDotNet [Baseline] here since
    /// this is a single-method run - compare its Mean/Allocated numbers directly against
    /// Command_Mediator's own run from the main Innovation.Benchmarks.Comparison project, both recorded
    /// in BENCHMARKS.md.
    /// </summary>
    [MemoryDiagnoser]
    public class MediatorScopedComparisonTests
    {
        private IMediator mediator;

        private static readonly ScopedCommand command = new ScopedCommand();

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.mediator = new MediatorScopedProvider().GetRequiredService<IMediator>();
        }

        [Benchmark]
        public async ValueTask<ScopedCommandResult> Command_Mediator_Scoped()
        {
            return await this.mediator.Send(command);
        }
    }

    /// <summary>
    /// Measures Mediator (martinothamar) dispatching a single, IO-free command through a validation +
    /// logging pipeline behavior (same behaviors as MediatorLoadedProvider in the main comparison
    /// project), under ServiceLifetime.Scoped - so it is a fair comparison against
    /// Command_Mediator_Loaded (Singleton), not just against the behavior-free Command_Mediator_Scoped
    /// above. There is deliberately no BenchmarkDotNet [Baseline] here since this is a single-method
    /// run - compare its Mean/Allocated numbers directly against Command_Mediator_Loaded's own run
    /// from the main Innovation.Benchmarks.Comparison project, both recorded in BENCHMARKS.md.
    /// </summary>
    [MemoryDiagnoser]
    public class MediatorScopedLoadedComparisonTests
    {
        private IMediator mediator;

        private static readonly LoadedScopedCommand command = new LoadedScopedCommand();

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.mediator = new MediatorScopedLoadedProvider().GetRequiredService<IMediator>();
        }

        [Benchmark]
        public async ValueTask<LoadedScopedCommandResult> Command_Mediator_Scoped_Loaded()
        {
            return await this.mediator.Send(command);
        }
    }

    /// <summary>
    /// Measures Command_Mediator_Scoped_Loaded's cost when a new DI scope is created and a fresh
    /// IMediator is resolved on every call, instead of resolving IMediator once in GlobalSetup and
    /// reusing it (as MediatorScopedLoadedComparisonTests does). GlobalSetup-once resolution means the
    /// Scoped-registered handler/behaviors are constructed exactly once for the whole benchmark run and
    /// reused on every call (a Scoped service resolved repeatedly from a root ServiceProvider with no
    /// intervening CreateScope() behaves like a Singleton - see BENCHMARKS.md, "Per-request scope
    /// cost", for the verified proof of this). Creating a new scope per call instead mirrors how
    /// ASP.NET Core actually behaves in production: one new DI scope per HTTP request, so the
    /// Scoped-registered handler/behaviors are constructed fresh on every request.
    ///
    /// This class exists to be directly, provably comparable to
    /// LoadedCommandPerRequestComparisonTests.Command_Innovation_Loaded_PerRequest in the main
    /// Innovation.Benchmarks.Comparison project, which applies the identical CreateScope-per-call
    /// pattern to Innovation's IDispatcher. See BENCHMARKS.md, "Per-request scope cost", for the full
    /// rationale and results.
    /// </summary>
    [MemoryDiagnoser]
    public class MediatorScopedLoadedPerRequestComparisonTests
    {
        private MediatorScopedLoadedProvider provider;

        private static readonly LoadedScopedCommand command = new LoadedScopedCommand();

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.provider = new MediatorScopedLoadedProvider();
        }

        [Benchmark]
        public async ValueTask<LoadedScopedCommandResult> Command_Mediator_Scoped_Loaded_PerRequest()
        {
            using var scope = this.provider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            return await mediator.Send(command);
        }
    }
}
