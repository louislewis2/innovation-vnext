namespace Innovation.ServiceBus.InProcess.Tests
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;

    using Innovation.Api.vNext.Dispatching;
    using Innovation.ServiceBus.InProcess.vNext.Settings;

    public class TestBase
    {
        #region Fields

        // This assembly contains deliberately handler-less commands/queries used by the missing-handler tests.
        internal static readonly string[] TestAssemblyNames = new[] { "Innovation.ServiceBus.InProcess.Tests" };

        #endregion Fields

        #region Constructor

        public TestBase() : this(configureOptions: null)
        {
        }

        // Allows derived test classes to opt into non-default InnovationOptions (e.g. AggregateValidationErrors)
        // without every other test having to know about it.
        protected TestBase(Action<InnovationOptions> configureOptions)
        {
            var services = new ServiceCollection();
            services.AddLogging(config =>
            {
                config.SetMinimumLevel(LogLevel.Debug);
                config.AddDebug();
                config.AddConsole();
                config.AddDebug();
            });

            services.AddOptions();
            services.AddConsumer();

            if (configureOptions == null)
            {
                services.AddInnovationvNext(innovationOptions =>
                {
                    innovationOptions.IsValidationEnabled = true;
                    innovationOptions.IgnoreMissingHandlersFromAssemblies = TestAssemblyNames;
                });
            }
            else
            {
                services.AddInnovationvNext(innovationOptions =>
                {
                    innovationOptions.IsValidationEnabled = true;
                    innovationOptions.IgnoreMissingHandlersFromAssemblies = TestAssemblyNames;
                    configureOptions(innovationOptions);
                });
            }

            this.ServiceProvider = services.BuildServiceProvider();
        }

        #endregion Constructor

        #region Properties

        public IServiceProvider ServiceProvider { get; private set; }

        #endregion Properties

        #region Methods

        internal TSource GetService<TSource>()
        {
            return this.ServiceProvider.GetRequiredService<TSource>();
        }

        internal IDispatcher GetDispatcher()
        {
            return this.GetService<IDispatcher>();
        }

        #endregion Methods
    }
}
