namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using System.Threading;
    using System.Threading.Tasks;

    using global::Mediator;

    /// <summary>
    /// Second of two IO-free BlankMessage notification handlers - see BlankMessageHandlerOne.
    /// </summary>
    public class BlankMessageHandlerTwo : INotificationHandler<BlankMessage>
    {
        public ValueTask Handle(BlankMessage notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
