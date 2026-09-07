namespace Innovation.ServiceBus.InProcess.vNext.Exceptions
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Thrown from InnovationRuntime.Configure() (i.e. during service registration/app startup, never on a
    /// per-request path) when InnovationOptions.FailFastOnMissingHandlers is true and one or more discovered
    /// ICommand/IQuery types have no corresponding handler registered anywhere in the scanned assemblies.
    /// Surfacing this at startup means a missing handler is caught immediately, instead of only being
    /// discovered the first time that specific command/query is actually dispatched in production.
    /// </summary>
    public class MissingHandlersException : Exception
    {
        #region Fields

        private readonly IReadOnlyList<Type> commandTypesWithoutHandlers;
        private readonly IReadOnlyList<Type> queryTypesWithoutHandlers;

        #endregion Fields

        #region Constructor

        public MissingHandlersException(IReadOnlyList<Type> commandTypesWithoutHandlers, IReadOnlyList<Type> queryTypesWithoutHandlers)
            : base(message: BuildMessage(commandTypesWithoutHandlers: commandTypesWithoutHandlers, queryTypesWithoutHandlers: queryTypesWithoutHandlers))
        {
            this.commandTypesWithoutHandlers = commandTypesWithoutHandlers ?? Array.Empty<Type>();
            this.queryTypesWithoutHandlers = queryTypesWithoutHandlers ?? Array.Empty<Type>();
        }

        #endregion Constructor

        #region Properties

        public IReadOnlyList<Type> CommandTypesWithoutHandlers => this.commandTypesWithoutHandlers;
        public IReadOnlyList<Type> QueryTypesWithoutHandlers => this.queryTypesWithoutHandlers;

        #endregion Properties

        #region Private Methods

        private static string BuildMessage(IReadOnlyList<Type> commandTypesWithoutHandlers, IReadOnlyList<Type> queryTypesWithoutHandlers)
        {
            var messageParts = new List<string>
            {
                "One or more ICommand/IQuery types have no registered handler."
            };

            if (commandTypesWithoutHandlers != null && commandTypesWithoutHandlers.Count > 0)
            {
                messageParts.Add($"Commands without a handler: {string.Join(", ", commandTypesWithoutHandlers.Select(x => x.FullName))}");
            }

            if (queryTypesWithoutHandlers != null && queryTypesWithoutHandlers.Count > 0)
            {
                messageParts.Add($"Queries without a handler: {string.Join(", ", queryTypesWithoutHandlers.Select(x => x.FullName))}");
            }

            return string.Join(" ", messageParts);
        }

        #endregion Private Methods
    }
}
