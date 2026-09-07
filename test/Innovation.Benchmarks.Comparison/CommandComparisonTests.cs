namespace Innovation.Benchmarks.Comparison
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Dispatching;

    using MediatRSender = MediatR.ISender;
    using MediatorMediator = global::Mediator.IMediator;

    /// <summary>
    /// Compares dispatching a single, IO-free command (with a result) through Innovation, MediatR, and
    /// Mediator (martinothamar). Each library resolves its entry point once in GlobalSetup, matching the
    /// steady-state style of the existing Innovation.Benchmarks project (dispatcher/mediator already
    /// resolved, not measuring DI-resolution/cold-start cost). Audit store and validation are disabled
    /// on the Innovation side since neither MediatR nor Mediator offer an equivalent out of the box.
    ///
    /// Mediator under ServiceLifetime.Scoped is measured separately, in the standalone
    /// Innovation.Benchmarks.Comparison.MediatorScoped console project - it cannot live in this project
    /// alongside MediatorLib's Singleton registration, because Mediator's ServiceLifetime option is a
    /// compile-time, assembly-wide source generator setting (see BENCHMARKS.md, "Confirming what Scoped
    /// mode actually generates", and that project's README for why a second assembly wasn't enough on
    /// its own either - the two assemblies' generators still collide if referenced together).
    /// </summary>
    [MemoryDiagnoser]
    public class CommandComparisonTests
    {
        private IDispatcher innovationDispatcher;
        private MediatRSender mediatRSender;
        private MediatorMediator mediator;

        private static readonly InnovationLib.BlankCommand innovationCommand = new InnovationLib.BlankCommand();
        private static readonly MediatRLib.BlankCommand mediatRCommand = new MediatRLib.BlankCommand();
        private static readonly MediatorLib.BlankCommand mediatorCommand = new MediatorLib.BlankCommand();

        [GlobalSetup]
        public void GlobalSetup()
        {
            var innovationProvider = new InnovationProvider();

            this.innovationDispatcher = innovationProvider.GetRequiredService<IDispatcher>();
            this.mediatRSender = new MediatRProvider().GetRequiredService<MediatRSender>();
            this.mediator = new MediatorProvider().GetRequiredService<MediatorMediator>();
        }

        [Benchmark(Baseline = true)]
        public async ValueTask<ICommandResult> Command_Innovation()
        {
            return await this.innovationDispatcher.Command(command: innovationCommand);
        }

        [Benchmark]
        public async Task<MediatRLib.BlankCommandResult> Command_MediatR()
        {
            return await this.mediatRSender.Send(mediatRCommand);
        }

        [Benchmark]
        public async ValueTask<MediatorLib.BlankCommandResult> Command_Mediator()
        {
            return await this.mediator.Send(mediatorCommand);
        }
    }
}

