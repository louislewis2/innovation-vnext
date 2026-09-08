namespace Innovation.SampleApi.Consumer.Handlers.ReactorTest
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Core;
    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.CommandHelpers;

    using Innovation.ApiSample;

    /// <summary>
    /// Implements ICorrelationAware on the handler - the dispatcher sets the correlation id here, not on
    /// the command, so this also serves as the regression test for that behavior.
    /// </summary>
    public class ReactorTestCommandHandler : ICommandHandler<ReactorTestCommand>, ICorrelationAware
    {
        #region Fields

        private static readonly ICommandResult commandResult = new CommandResult();
        private readonly ScopeMarker scopeMarker;

        #endregion Fields

        #region Constructor

        public ReactorTestCommandHandler(ScopeMarker scopeMarker)
        {
            this.scopeMarker = scopeMarker;
        }

        #endregion Constructor

        #region Properties

        public string CorrelationId { private get; set; }

        #endregion Properties

        #region Methods

        public ValueTask<ICommandResult> Handle(ReactorTestCommand command)
        {
            ReactorTestSignal.RecordDispatchScopeId(correlationId: this.CorrelationId, scopeId: this.scopeMarker.Id);

            return ValueTask.FromResult(commandResult);
        }

        #endregion Methods
    }
}
