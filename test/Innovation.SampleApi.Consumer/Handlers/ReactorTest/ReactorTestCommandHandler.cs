namespace Innovation.SampleApi.Consumer.Handlers.ReactorTest
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.CommandHelpers;

    using Innovation.ApiSample;

    public class ReactorTestCommandHandler : ICommandHandler<ReactorTestCommand>
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

        #region Methods

        public ValueTask<ICommandResult> Handle(ReactorTestCommand command)
        {
            ReactorTestSignal.RecordDispatchScopeId(correlationId: command.CorrelationId, scopeId: this.scopeMarker.Id);

            return ValueTask.FromResult(commandResult);
        }

        #endregion Methods
    }
}
