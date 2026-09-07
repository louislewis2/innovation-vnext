namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using MediatR;

    /// <summary>
    /// First of two IO-free BlankMessage notification handlers - see BlankMessageHandlerTwo.
    /// </summary>
    public class BlankMessageHandlerOne : INotificationHandler<BlankMessage>
    {
        public Task Handle(BlankMessage notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
