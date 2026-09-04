namespace Innovation.Benchmarks
{
    using System;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;

    using Innovation.ServiceBus.InProcess.vNext.Settings;

    public class DependencyBuilderBase
    {
        #region Fields

        private ServiceProvider serviceProvider;

        #endregion Fields

        #region Constructor

        public DependencyBuilderBase() : this(includeAuditStore: false, configureOptions: null)
        {
        }

        /// <summary>
        /// Allows derived benchmark classes to build a provider shaped differently to the default (e.g.
        /// with an IAuditStore registered, or with AggregateValidationErrors enabled) so pipeline on/off
        /// comparisons can be benchmarked without duplicating the base wiring. The parameterless
        /// constructor deliberately leaves IAuditStore unregistered - benchmarks using it (Blank,
        /// DispatcherPipelineComparisonTests, etc.) are meant to represent the framework's true
        /// zero-registration floor. Audit-store cost is only ever measured explicitly, via
        /// AuditStoreComparisonTests, so it never silently inflates every other benchmark's numbers.
        /// </summary>
        protected DependencyBuilderBase(bool includeAuditStore, Action<InnovationOptions> configureOptions)
        {
            this.ConfigureServices(includeAuditStore: includeAuditStore, configureOptions: configureOptions);
        }

        #endregion Constructor

        #region Methods

        public T GetRequiredService<T>()
        {
            return this.serviceProvider.GetService<T>();
        }

        #endregion Methods

        #region Private Methods

        private void ConfigureServices(bool includeAuditStore, Action<InnovationOptions> configureOptions)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(options => { options.SetMinimumLevel(LogLevel.Warning); });
            serviceCollection.AddConsumer(includeAuditStore: includeAuditStore);
            serviceCollection.AddInnovationvNext(innovationOptions =>
            {
                innovationOptions.IsValidationEnabled = true;
                configureOptions?.Invoke(innovationOptions);
            });

            this.serviceProvider = serviceCollection.BuildServiceProvider();
        }

        #endregion Private Methods
    }
}
