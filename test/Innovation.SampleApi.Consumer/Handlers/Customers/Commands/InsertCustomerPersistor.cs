namespace Innovation.SampleApi.Consumer.Handlers.Customers.Commands
{
    using System.Threading;
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.CommandHelpers;

    using ApiSample.Customers.Commands;

    public class InsertCustomerPersistor : ICommandHandler<InsertCustomerCommand>
    {
        #region Methods

        public async ValueTask<ICommandResult> Handle(InsertCustomerCommand command, CancellationToken cancellationToken)
        {
            return await this.Persist(command: command, cancellationToken: cancellationToken);
        }

        #endregion Methods

        #region Private Methods

        private async Task<ICommandResult> Persist(InsertCustomerCommand command, CancellationToken cancellationToken)
        {
            return await Task.FromResult(result: new CommandResult());
        }

        #endregion Private Methods
    }
}
