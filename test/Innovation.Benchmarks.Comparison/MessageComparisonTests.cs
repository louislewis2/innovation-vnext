namespace Innovation.Benchmarks.Comparison
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;

    using Innovation.Api.vNext.Dispatching;

    using MediatRPublisher = MediatR.IPublisher;
    using MediatorMediator = global::Mediator.IMediator;

    /// <summary>
    /// Compares broadcasting a single, IO-free message/notification to two registered handlers through
    /// Innovation (Dispatcher.Message), MediatR (IPublisher.Publish), and Mediator/martinothamar
    /// (IMediator.Publish). See CommandComparisonTests for the shared setup rationale.
    /// </summary>
    [MemoryDiagnoser]
    public class MessageComparisonTests
    {
        private IDispatcher innovationDispatcher;
        private MediatRPublisher mediatRPublisher;
        private MediatorMediator mediator;

        private static readonly InnovationLib.BlankMessage innovationMessage = new InnovationLib.BlankMessage();
        private static readonly MediatRLib.BlankMessage mediatRMessage = new MediatRLib.BlankMessage();
        private static readonly MediatorLib.BlankMessage mediatorMessage = new MediatorLib.BlankMessage();

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.innovationDispatcher = new InnovationProvider().GetRequiredService<IDispatcher>();
            this.mediatRPublisher = new MediatRProvider().GetRequiredService<MediatRPublisher>();
            this.mediator = new MediatorProvider().GetRequiredService<MediatorMediator>();
        }

        [Benchmark(Baseline = true)]
        public async ValueTask Message_Innovation()
        {
            await this.innovationDispatcher.Message(message: innovationMessage, cancellationToken: System.Threading.CancellationToken.None);
        }

        [Benchmark]
        public async Task Message_MediatR()
        {
            await this.mediatRPublisher.Publish(mediatRMessage);
        }

        [Benchmark]
        public async ValueTask Message_Mediator()
        {
            await this.mediator.Publish(mediatorMessage);
        }
    }
}
