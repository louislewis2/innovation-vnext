namespace Innovation.SampleApi.Consumer.Handlers
{
    using System.Threading;
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.CommandHelpers;

    using Innovation.ApiSample;

    public class BlankCommandPersistor : ICommandHandler<BlankCommand>
    {
        #region Fields

        private static readonly ICommandResult commandResult = new CommandResult();

        #endregion Fields

        #region Methods

        public ValueTask<ICommandResult> Handle(BlankCommand command, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(commandResult);
        }

        #endregion Methods
    }
}
