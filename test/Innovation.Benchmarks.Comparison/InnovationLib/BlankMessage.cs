namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using Innovation.Api.vNext.Messaging;

    /// <summary>
    /// Mirrors Innovation.ApiSample.BlankMessage - an IO-free message used to exercise the
    /// broadcast-to-multiple-handlers path (Dispatcher.Message), analogous to MediatR/Mediator
    /// notifications with two handlers registered.
    /// </summary>
    public class BlankMessage : IMessage
    {
        public string EventName => nameof(BlankMessage);
    }
}
