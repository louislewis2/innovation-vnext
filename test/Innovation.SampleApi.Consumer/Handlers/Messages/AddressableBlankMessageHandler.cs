namespace Innovation.SampleApi.Consumer.Handlers.Messages
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Messaging;

    using Innovation.ApiSample;

    /// <summary>
    /// An addressable BlankMessage handler, resolved by Dispatcher.MessageFor when dispatched with the
    /// "benchmark" address. Exists alongside BlankMessageHandler so MessageFor's extra
    /// OfType/Where/Cast/Intersect filtering step can be benchmarked separately from Message's plain
    /// broadcast-to-all-handlers path.
    /// </summary>
    public class AddressableBlankMessageHandler : IMessageHandler<BlankMessage>, IAddressable
    {
        #region Fields

        private static readonly string[] handles = { "benchmark" };

        #endregion Fields

        #region Properties

        public string[] Handles => handles;

        #endregion Properties

        #region Methods

        public ValueTask Handle(BlankMessage message)
        {
            return ValueTask.CompletedTask;
        }

        #endregion Methods
    }
}
