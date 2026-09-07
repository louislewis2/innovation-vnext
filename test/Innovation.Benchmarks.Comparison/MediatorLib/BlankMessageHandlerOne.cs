namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    /// <summary>
    /// First of two IO-free BlankMessage notification handlers - see BlankMessageHandlerTwo.
    /// </summary>
    public class BlankMessageHandlerOne : INotificationHandler<BlankMessage>
    {
        public ValueTask Handle(BlankMessage notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
