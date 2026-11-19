namespace Innovation.ServiceBus.InProcess.Tests
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Innovation.ServiceBus.InProcess.vNext.Enums;
    using Innovation.ServiceBus.InProcess.vNext.Settings;
    using Innovation.ServiceBus.InProcess.vNext.Exceptions;

    [TestClass]
    public class MissingHandlerTests
    {
        #region Methods

        [TestMethod]
        public void Throw_Logs_Error_And_Prevents_Startup()
        {
            // Arrange
            var loggerProvider = new CapturingLoggerProvider();

            // Act
            Assert.ThrowsException<MissingHandlersException>(() => this.Configure(loggerProvider: loggerProvider, configureOptions: options => options.MissingHandlers = MissingHandlerBehavior.Throw));

            // Assert
            Assert.IsTrue(loggerProvider.Entries.Any(x => x.LogLevel == LogLevel.Error && x.Message.Contains(nameof(UnhandledTestCommand))));
        }

        [TestMethod]
        public void Unknown_Is_Treated_As_Throw()
        {
            // Arrange
            var loggerProvider = new CapturingLoggerProvider();

            // Act / Assert
            Assert.ThrowsException<MissingHandlersException>(() => this.Configure(loggerProvider: loggerProvider, configureOptions: options => options.MissingHandlers = MissingHandlerBehavior.Unknown));
        }

        [TestMethod]
        public void Warn_Logs_Single_Warning_And_No_Errors()
        {
            // Arrange
            var loggerProvider = new CapturingLoggerProvider();

            // Act
            this.Configure(loggerProvider: loggerProvider, configureOptions: options => options.MissingHandlers = MissingHandlerBehavior.Warn);

            // Assert
            Assert.AreEqual(expected: 1, actual: loggerProvider.Entries.Count(x => x.LogLevel == LogLevel.Warning && x.Message.Contains("have no registered handler")));
            Assert.AreEqual(expected: 0, actual: loggerProvider.Entries.Count(x => x.LogLevel >= LogLevel.Error));
            Assert.IsTrue(loggerProvider.Entries.Any(x => x.LogLevel == LogLevel.Debug && x.Message.Contains(nameof(UnhandledTestCommand))));
        }

        [TestMethod]
        public void Ignore_Logs_No_Warnings_Or_Errors()
        {
            // Arrange
            var loggerProvider = new CapturingLoggerProvider();

            // Act
            this.Configure(loggerProvider: loggerProvider, configureOptions: options => options.MissingHandlers = MissingHandlerBehavior.Ignore);

            // Assert
            Assert.AreEqual(expected: 0, actual: loggerProvider.Entries.Count(x => x.LogLevel >= LogLevel.Warning));
        }

        [TestMethod]
        public void Ignored_Assembly_Is_Not_Reported_Even_When_Throwing()
        {
            // Arrange
            var loggerProvider = new CapturingLoggerProvider();

            // Act
            this.Configure(
                loggerProvider: loggerProvider,
                configureOptions: options =>
                {
                    options.MissingHandlers = MissingHandlerBehavior.Throw;
                    options.IgnoreMissingHandlersFromAssemblies = new[] { "innovation.servicebus.inprocess.tests" };
                });

            // Assert
            Assert.AreEqual(expected: 0, actual: loggerProvider.Entries.Count(x => x.LogLevel >= LogLevel.Warning));
        }

        #endregion Methods

        #region Private Methods

        private void Configure(CapturingLoggerProvider loggerProvider, Action<InnovationOptions> configureOptions)
        {
            var services = new ServiceCollection();

            services.AddLogging(config =>
            {
                config.SetMinimumLevel(LogLevel.Debug);
                config.AddProvider(loggerProvider);
            });

            services.AddOptions();
            services.AddConsumer();
            services.AddInnovationvNext(configureOptions);
        }

        #endregion Private Methods
    }
}