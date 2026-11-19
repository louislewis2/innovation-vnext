namespace Innovation.ServiceBus.InProcess.vNext.Enums
{
    /// <summary>
    /// Determines what Innovation does at startup when a discovered command or query has no registered handler.
    /// </summary>
    public enum MissingHandlerBehavior
    {
        /// <summary>
        /// Not set. Treated as <see cref="Throw"/>, so the check is never silently disabled.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Logs an error per missing handler and throws a MissingHandlersException, preventing the application from starting.
        /// </summary>
        Throw = 1,

        /// <summary>
        /// Logs a single summary warning (with the affected assemblies) and lets the application start.
        /// The per-type detail is written at Debug level.
        /// </summary>
        Warn = 2,

        /// <summary>
        /// Lets the application start without a warning. The per-type detail is still written at Debug level.
        /// </summary>
        Ignore = 3
    }
}