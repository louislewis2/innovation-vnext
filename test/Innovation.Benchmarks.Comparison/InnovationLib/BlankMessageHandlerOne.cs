namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Messaging;

    /// <summary>
    /// First of two IO-free BlankMessage handlers, so Message dispatch broadcasts to more than one
    /// handler - matching the shape of the MediatR/Mediator notification benchmarks (two handlers each).
    /// </summary>
    public class BlankMessageHandlerOne : IMessageHandler<BlankMessage>
    {
        public ValueTask Handle(BlankMessage message, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
