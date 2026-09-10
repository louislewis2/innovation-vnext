namespace Innovation.SampleApi.Consumer.Handlers.Messages
{
    using System.Threading;
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Messaging;

    using Innovation.ApiSample;

    /// <summary>
    /// Handles BlankMessage as a plain (non-addressable) broadcast handler - resolved by Dispatcher.Message,
    /// which invokes every registered IMessageHandler&lt;BlankMessage&gt;. Kept IO-free so Message dispatch
    /// benchmarks measure only framework overhead.
    /// </summary>
    public class BlankMessageHandler : IMessageHandler<BlankMessage>
    {
        #region Methods

        public ValueTask Handle(BlankMessage message, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        #endregion Methods
    }
}
