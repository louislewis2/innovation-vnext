namespace Innovation.Benchmarks.PipelineTests
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;
    using Innovation.Api.vNext.Dispatching;

    using Innovation.ApiSample;

    /// <summary>
    /// Benchmarks Dispatcher.Message (broadcasts to every registered IMessageHandler&lt;BlankMessage&gt;)
    /// and Dispatcher.MessageFor (filters handlers down to those addressable to a given set of
    /// addresses via IAddressable) end-to-end. Two IO-free handlers are registered for BlankMessage -
    /// BlankMessageHandler (plain) and AddressableBlankMessageHandler (addressable, "benchmark") - so
    /// Message exercises the broadcast-to-both path and MessageFor exercises the
    /// OfType/Where/Cast/Intersect filtering path in isolation.
    /// </summary>
    [MemoryDiagnoser]
    public class DispatcherMessageTests : DependencyBuilderBase
    {
        #region Fields

        private IDispatcher dispatcher;
        private static readonly BlankMessage blankMessage = new BlankMessage();
        private static readonly string[] addresses = { "benchmark" };

        #endregion Fields

        #region Methods

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.dispatcher = this.GetRequiredService<IDispatcher>();
        }

        [Benchmark(Baseline = true)]
        public async ValueTask DispatchMessage()
        {
            await dispatcher.Message(message: blankMessage, cancellationToken: System.Threading.CancellationToken.None);
        }

        [Benchmark]
        public async ValueTask DispatchMessageFor()
        {
            await dispatcher.MessageFor(message: blankMessage, cancellationToken: System.Threading.CancellationToken.None, addresses: addresses);
        }

        #endregion Methods
    }
}
