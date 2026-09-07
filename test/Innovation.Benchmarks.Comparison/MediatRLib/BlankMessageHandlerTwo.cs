namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using MediatR;

    /// <summary>
    /// Second of two IO-free BlankMessage notification handlers - see BlankMessageHandlerOne.
    /// </summary>
    public class BlankMessageHandlerTwo : INotificationHandler<BlankMessage>
    {
        public Task Handle(BlankMessage notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
