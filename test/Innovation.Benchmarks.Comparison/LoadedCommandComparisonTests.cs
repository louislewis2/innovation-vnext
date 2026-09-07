namespace Innovation.Benchmarks.Comparison
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Dispatching;

    using MediatRSender = MediatR.ISender;
    using MediatorMediator = global::Mediator.IMediator;

    /// <summary>
    /// Compares dispatching a single, IO-free command through Innovation, MediatR, and Mediator
    /// (martinothamar) with an equivalent pair of cross-cutting concerns enabled on every library:
    /// a no-op validation step, then a no-op logging step.
    ///
    /// CommandComparisonTests deliberately disables Innovation's validation/audit (since MediatR and
    /// Mediator ship with none of that out of the box) to isolate raw dispatch cost - a "micro vs micro"
    /// comparison. That's a fair like-for-like measurement, but it understates what most real
    /// applications actually configure: Innovation ships validation/audit/interceptor/reactor hooks by
    /// default, whereas MediatR/Mediator require consumers to hand-write and register
    /// IPipelineBehavior&lt;,&gt; implementations for the same concerns.
    ///
    /// This class levels the playing field the other way - "framework vs framework" - by adding a
    /// validation behavior and a logging behavior to MediatR and Mediator (registered the same way their
    /// own documentation recommends: open generics via IPipelineBehavior&lt;,&gt;), and by enabling
    /// Innovation's IValidator&lt;T&gt; + IAuditStore for the equivalent command.
    ///
    /// See BENCHMARKS.md in this project for the full write-up of what these numbers mean and don't mean.
    /// </summary>
    [MemoryDiagnoser]
    public class LoadedCommandComparisonTests
    {
        private IDispatcher innovationDispatcher;
        private MediatRSender mediatRSender;
        private MediatorMediator mediator;

        private static readonly InnovationLib.LoadedCommand innovationCommand = new InnovationLib.LoadedCommand();
        private static readonly MediatRLib.LoadedCommand mediatRCommand = new MediatRLib.LoadedCommand();
        private static readonly MediatorLib.LoadedCommand mediatorCommand = new MediatorLib.LoadedCommand();

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.innovationDispatcher = new InnovationLoadedProvider().GetRequiredService<IDispatcher>();
            this.mediatRSender = new MediatRLoadedProvider().GetRequiredService<MediatRSender>();
            this.mediator = new MediatorLoadedProvider().GetRequiredService<MediatorMediator>();
        }

        [Benchmark(Baseline = true)]
        public async ValueTask<ICommandResult> Command_Innovation_Loaded()
        {
            return await this.innovationDispatcher.Command(command: innovationCommand);
        }

        [Benchmark]
        public async Task<MediatRLib.LoadedCommandResult> Command_MediatR_Loaded()
        {
            return await this.mediatRSender.Send(mediatRCommand);
        }

        [Benchmark]
        public async ValueTask<MediatorLib.LoadedCommandResult> Command_Mediator_Loaded()
        {
            return await this.mediator.Send(mediatorCommand);
        }
    }
}
