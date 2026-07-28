namespace Innovation.ApiSample
{
    using Innovation.Api.vNext.Commanding;

    public class BlankCommand : ICommand
    {
        public string EventName => nameof(BlankCommand);
    }
}
