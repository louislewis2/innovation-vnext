namespace Innovation.SampleApi.Consumer.Handlers.ReactorTest
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Core;
    using Innovation.Api.vNext.Reactions;
    using Innovation.Api.vNext.Commanding;

    using Innovation.ApiSample;

    public class ReactorTestCommandResultReactor : ICommandResultReactor<ReactorTestCommand>, ICorrelationAware
    {
        #region Fields

        private readonly ScopeMarker scopeMarker;

        #endregion Fields

        #region Constructor

        public ReactorTestCommandResultReactor(ScopeMarker scopeMarker)
        {
            this.scopeMarker = scopeMarker;
        }

        #endregion Constructor

        #region Properties

        public string CorrelationId { private get; set; }

        #endregion Properties

        #region Methods

        public Task React(ICommandResult commandResult, ReactorTestCommand command)
        {
            ReactorTestSignal.SignalCommandResultReactor(correlationId: this.CorrelationId, reactorScopeId: this.scopeMarker.Id);

            return Task.CompletedTask;
        }

        #endregion Methods
    }
}
