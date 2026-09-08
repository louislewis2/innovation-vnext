namespace Innovation.ApiSample
{
    using Innovation.Api.vNext.Commanding;

    /// <summary>
    /// A dedicated command used only to exercise the reactor pipeline (ICommandReactor/ICommandResultReactor)
    /// in isolation from the other sample commands, so reactor-pipeline tests don't interfere with (or
    /// depend on) unrelated test fixtures.
    /// </summary>
    public class ReactorTestCommand : ICommand
    {
        public string EventName => nameof(ReactorTestCommand);
    }
}
