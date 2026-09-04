namespace Innovation.ApiSample
{
    using Innovation.Api.vNext.Messaging;

    /// <summary>
    /// A dedicated, IO-free message used only to exercise the Message/MessageFor dispatch pipeline in
    /// benchmarks, analogous to BlankCommand for the Command pipeline.
    /// </summary>
    public class BlankMessage : IMessage
    {
        public string EventName => nameof(BlankMessage);
    }
}
