namespace Innovation.SampleApi.Consumer.Handlers.ReactorTest
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Core;
    using Innovation.Api.vNext.Reactions;

    using Innovation.ApiSample;

    public class ReactorTestCommandReactor : ICommandReactor<ReactorTestCommand>, ICorrelationAware
    {
        #region Fields

        private readonly ScopeMarker scopeMarker;

        #endregion Fields

        #region Constructor

        public ReactorTestCommandReactor(ScopeMarker scopeMarker)
        {
            this.scopeMarker = scopeMarker;
        }

        #endregion Constructor

        #region Properties

        public string CorrelationId { private get; set; }

        #endregion Properties

        #region Methods

        public Task React(ReactorTestCommand command)
        {
            ReactorTestSignal.SignalCommandReactor(correlationId: this.CorrelationId, reactorScopeId: this.scopeMarker.Id);

            return Task.CompletedTask;
        }

        #endregion Methods
    }
}
