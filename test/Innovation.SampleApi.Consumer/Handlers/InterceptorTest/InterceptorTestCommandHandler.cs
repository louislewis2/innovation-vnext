namespace Innovation.SampleApi.Consumer.Handlers.InterceptorTest
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.CommandHelpers;

    using Innovation.ApiSample;

    /// <summary>
    /// Handles InterceptorTestCommand - kept deliberately free of any IO so interceptor-pipeline
    /// benchmarks measure only framework overhead.
    /// </summary>
    public class InterceptorTestCommandHandler : ICommandHandler<InterceptorTestCommand>
    {
        #region Fields

        private static readonly ICommandResult commandResult = new CommandResult();

        #endregion Fields

        #region Methods

        public ValueTask<ICommandResult> Handle(InterceptorTestCommand command)
        {
            return ValueTask.FromResult(commandResult);
        }

        #endregion Methods
    }
}
