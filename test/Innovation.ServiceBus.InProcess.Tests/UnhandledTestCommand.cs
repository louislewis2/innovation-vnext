namespace Innovation.ServiceBus.InProcess.Tests
{
    using Innovation.Api.vNext.Commanding;

    /// <summary>
    /// A command that deliberately has no handler, used to exercise the missing handler startup validation.
    /// </summary>
    public class UnhandledTestCommand : ICommand
    {
        #region Properties

        public string EventName => nameof(UnhandledTestCommand);

        #endregion Properties
    }
}