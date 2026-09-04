namespace Innovation.Benchmarks.PipelineTests
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Attributes;
    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Dispatching;

    using Innovation.ApiSample;

    /// <summary>
    /// Benchmarks the cost of registering an IAuditStore. AuditStoreNotRegistered is the baseline - it
    /// matches the framework's zero-registration floor used everywhere else (Blank, etc.) - so this class
    /// isolates exactly how much registering an audit store adds on top of that floor. The dispatcher
    /// checks for an IAuditStore on every dispatch regardless, but when one is present it also builds an
    /// AuditContext and awaits Log() for every command/query/message - worth tracking since most real
    /// consumers will register one. SampleAuditStore is in-memory only (a Dictionary keyed by correlation
    /// id) so this stays IO-free.
    /// </summary>
    [MemoryDiagnoser]
    public class AuditStoreComparisonTests
    {
        #region Fields

        private IDispatcher dispatcherWithAuditStore;
        private IDispatcher dispatcherWithoutAuditStore;
        private static readonly BlankCommand blankCommand = new BlankCommand();

        #endregion Fields

        #region Methods

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.dispatcherWithAuditStore = new AuditStoreEnabledProvider().GetRequiredService<IDispatcher>();
            this.dispatcherWithoutAuditStore = new AuditStoreDisabledProvider().GetRequiredService<IDispatcher>();
        }

        [Benchmark(Baseline = true)]
        public async ValueTask<ICommandResult> AuditStoreNotRegistered() => await dispatcherWithoutAuditStore.Command(command: blankCommand);

        [Benchmark]
        public async ValueTask<ICommandResult> AuditStoreRegistered() => await dispatcherWithAuditStore.Command(command: blankCommand);

        #endregion Methods

        #region Providers

        private sealed class AuditStoreEnabledProvider : DependencyBuilderBase
        {
            public AuditStoreEnabledProvider() : base(includeAuditStore: true, configureOptions: null)
            {
            }
        }

        private sealed class AuditStoreDisabledProvider : DependencyBuilderBase
        {
            public AuditStoreDisabledProvider() : base(includeAuditStore: false, configureOptions: null)
            {
            }
        }

        #endregion Providers
    }
}
