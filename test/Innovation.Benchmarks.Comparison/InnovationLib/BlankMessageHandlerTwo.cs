namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Messaging;

    /// <summary>
    /// Second of two IO-free BlankMessage handlers - see BlankMessageHandlerOne.
    /// </summary>
    public class BlankMessageHandlerTwo : IMessageHandler<BlankMessage>
    {
        public ValueTask Handle(BlankMessage message)
        {
            return ValueTask.CompletedTask;
        }
    }
}
