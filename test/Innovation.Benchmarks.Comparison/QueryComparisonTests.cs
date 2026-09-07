namespace Innovation.Benchmarks.Comparison
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;

    using Innovation.Api.vNext.Dispatching;

    using MediatRSender = MediatR.ISender;
    using MediatorMediator = global::Mediator.IMediator;

    /// <summary>
    /// Compares dispatching a single, IO-free query (request/response, single handler) through
    /// Innovation, MediatR, and Mediator (martinothamar). See CommandComparisonTests for the shared
    /// setup rationale (steady-state, audit store + validation disabled on the Innovation side).
    /// </summary>
    [MemoryDiagnoser]
    public class QueryComparisonTests
    {
        private IDispatcher innovationDispatcher;
        private MediatRSender mediatRSender;
        private MediatorMediator mediator;

        private static readonly InnovationLib.BlankQuery innovationQuery = new InnovationLib.BlankQuery();
        private static readonly MediatRLib.BlankQuery mediatRQuery = new MediatRLib.BlankQuery();
        private static readonly MediatorLib.BlankQuery mediatorQuery = new MediatorLib.BlankQuery();

        [GlobalSetup]
        public void GlobalSetup()
        {
            var innovationProvider = new InnovationProvider();

            this.innovationDispatcher = innovationProvider.GetRequiredService<IDispatcher>();
            this.mediatRSender = new MediatRProvider().GetRequiredService<MediatRSender>();
            this.mediator = new MediatorProvider().GetRequiredService<MediatorMediator>();
        }

        [Benchmark(Baseline = true)]
        public async ValueTask<InnovationLib.BlankQueryResult> Query_Innovation()
        {
            return await this.innovationDispatcher.Query<InnovationLib.BlankQuery, InnovationLib.BlankQueryResult>(query: innovationQuery);
        }

        [Benchmark]
        public async Task<MediatRLib.BlankQueryResult> Query_MediatR()
        {
            return await this.mediatRSender.Send(mediatRQuery);
        }

        [Benchmark]
        public async ValueTask<MediatorLib.BlankQueryResult> Query_Mediator()
        {
            return await this.mediator.Send(mediatorQuery);
        }
    }
}
