namespace Innovation.ServiceBus.InProcess.Tests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Records every log entry so tests can assert on the level and message that was written.
    /// </summary>
    public class CapturingLoggerProvider : ILoggerProvider, ILogger
    {
        #region Properties

        public List<(LogLevel LogLevel, string Message)> Entries { get; } = new List<(LogLevel LogLevel, string Message)>();

        #endregion Properties

        #region Methods

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.Entries.Add((logLevel, formatter(state, exception)));
        }

        #endregion Methods
    }
}