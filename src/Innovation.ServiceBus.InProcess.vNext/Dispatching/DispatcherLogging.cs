namespace Innovation.ServiceBus.InProcess.vNext.Dispatching
{
    using System;
    using Microsoft.Extensions.Logging;

    using Api.vNext.Commanding;

    internal static class DispatcherLogging
    {
        #region Fields

        public static readonly EventId commandEventId = new EventId(1, "Command");
        public static readonly EventId queryEventId = new EventId(2, "Query");
        public static readonly EventId messageEventId = new EventId(3, "Message");

        private static readonly Action<ILogger, Exception> commandParameterNull = LoggerMessage.Define(
            LogLevel.Error,
            commandEventId,
            "Command Cannot Be Null");

        private static readonly Action<ILogger, string, string, Type, Exception> contextNotSet = LoggerMessage.Define<string, string, Type>(
            LogLevel.Warning,
            commandEventId,
            "{EventName} Is Context Aware, Context Was Not Set.{correlationId} - {EventType}");

        private static readonly Action<ILogger, Type, Exception> auditStoreFound = LoggerMessage.Define<Type>(
            LogLevel.Debug,
            commandEventId,
            "Audit Store Found - {AuditStoreType}");

        private static readonly Action<ILogger, string, string, Type, Exception> enteredCommandDispatcher = LoggerMessage.Define<string, string, Type>(
            LogLevel.Debug,
            commandEventId,
            "Entered Command Dispatcher. {correlationId} - {CommandName} - {CommandType}");

        private static readonly Action<ILogger, ICommand, Exception> commandDetails = LoggerMessage.Define<ICommand>(
            LogLevel.Trace,
            commandEventId,
            "Command Details {@Command}");

        private static readonly Action<ILogger, string, Type, Exception> commandHandlerNotFound = LoggerMessage.Define<string, Type>(
            LogLevel.Error,
            commandEventId,
            "Command Handler Not Found - {CommandName} - {CommandType}");

        private static readonly Action<ILogger, Type, Exception> commandHandlerFound = LoggerMessage.Define<Type>(
            LogLevel.Debug,
            commandEventId,
            "Found Handler - {HandlerType}");

        private static readonly Action<ILogger, int, Exception> commandValidatorsFound = LoggerMessage.Define<int>(
            LogLevel.Debug,
            commandEventId,
            "Found {CommandValidatorCount} Command Validators");

        private static readonly Action<ILogger, Exception> notifyingCommandReactors = LoggerMessage.Define(
            LogLevel.Debug,
            commandEventId,
            "Enqueuing Command Reactors");

        private static readonly Action<ILogger, int, Exception> commandInterceptorsFound = LoggerMessage.Define<int>(
            LogLevel.Debug,
            commandEventId,
            "Found {CommandInterceptorCount} Command Interceptors");

        private static readonly Action<ILogger, Type, Exception> commandInterceptorGoingToRun = LoggerMessage.Define<Type>(
            LogLevel.Debug,
            commandEventId,
            "The Command Interceptor: {CommandInterceptorType} Is About To Get Run");

        private static readonly Action<ILogger, Type, Exception> commandInterceptorRaisedException = LoggerMessage.Define<Type>(
            LogLevel.Debug,
            commandEventId,
            "The Command Interceptor: {CommandInterceptorType} Raised An Exception");

        private static readonly Action<ILogger, string, bool, Exception> commandInitialValidationResult = LoggerMessage.Define<string, bool>(
            LogLevel.Debug,
            commandEventId,
            "Command {CommandName} Initial Validation Result: {Status}");

        private static readonly Action<ILogger, Exception> notifyingCommandResultReactors = LoggerMessage.Define(
            LogLevel.Debug,
            commandEventId,
            "Enqueuing Command Result Reactors");

        private static readonly Action<ILogger, bool,Exception> returningFromDispatcher = LoggerMessage.Define<bool>(
            LogLevel.Debug,
            commandEventId,
            "Returning From Dispatcher {FinalResult}");

        private static readonly Action<ILogger, Exception> queryParameterNull = LoggerMessage.Define(
            LogLevel.Error,
            queryEventId,
            "Query Cannot Be Null");

        private static readonly Action<ILogger, string, string, Type, Type, Exception> enteredQueryDispatcher = LoggerMessage.Define<string, string, Type, Type>(
            LogLevel.Debug,
            queryEventId,
            "Entered Query Dispatcher. {correlationId} - {QueryName} - {QueryType} - {ResultType}");

        private static readonly Action<ILogger, string, Type, Type, Exception> queryHandlerNotFound = LoggerMessage.Define<string, Type, Type>(
            LogLevel.Error,
            queryEventId,
            "Query Handler Not Found - {QueryName} - {QueryType} - {ResultType}");

        private static readonly Action<ILogger, Type, Type, Exception> queryHandlerFound = LoggerMessage.Define<Type, Type>(
            LogLevel.Debug,
            queryEventId,
            "Found Handler - {HandlerType} - {ResultType}");

        private static readonly Action<ILogger, string, Type, Type, string[], Exception> queryForHandlerNotFound = LoggerMessage.Define<string, Type, Type, string[]>(
            LogLevel.Error,
            queryEventId,
            "Query Handlers Not Found - {QueryName} - {QueryType} - {ResultType} - {Addresses}");

        private static readonly Action<ILogger, Exception> messageParameterNull = LoggerMessage.Define(
            LogLevel.Error,
            messageEventId,
            "Message Cannot Be Null");

        private static readonly Action<ILogger, string, string, Type, Exception> enteredMessageDispatcher = LoggerMessage.Define<string, string, Type>(
            LogLevel.Debug,
            messageEventId,
            "Entered Message Dispatcher. {correlationId} - {MessageName} - {MessageType}");

        private static readonly Action<ILogger, int, Exception> messageHandlersFound = LoggerMessage.Define<int>(
            LogLevel.Debug,
            messageEventId,
            "Found {MessageHandlerCount} Message Handlers");

        private static readonly Action<ILogger, string, Type, string[], Exception> messageForHandlersNotFound = LoggerMessage.Define<string, Type, string[]>(
            LogLevel.Error,
            messageEventId,
            "Addressable Message Handlers Not Found - {MessageName} - {MessageType} - {Addresses}");

        #endregion Fields

        #region Methods

        public static void CommandParameterNull(ILogger logger) => commandParameterNull(logger, null);
        public static void ContextNotSet(ILogger logger, string eventName, string correlationId, Type eventType) => contextNotSet(logger, eventName, correlationId, eventType, null);
        public static void AuditStoreFound(ILogger logger, Type auditStoreType) => auditStoreFound(logger, auditStoreType, null);
        public static void EnteredCommandDispatcher(ILogger logger, string correlationId, string commandName, Type commandType) => enteredCommandDispatcher(logger, correlationId, commandName, commandType, null);
        public static void CommandDetail(ILogger logger, ICommand command) => commandDetails(logger, command, null);
        public static void CommandHandlerNotFound(ILogger logger, string eventName, Type commandType) => commandHandlerNotFound(logger, eventName, commandType, null);
        public static void CommandHandlerFound(ILogger logger, Type commandHandlerType) => commandHandlerFound(logger, commandHandlerType, null);
        public static void CommandValidatorsFound(ILogger logger, int commandValidatorCount) => commandValidatorsFound(logger, commandValidatorCount, null);
        public static void NotifyingCommandReactors(ILogger logger) => notifyingCommandReactors(logger, null);
        public static void CommandInterceptorsFound(ILogger logger, int commandInterceptorCount) => commandInterceptorsFound(logger, commandInterceptorCount, null);
        public static void CommandInterceptorGoingToRun(ILogger logger, Type commandInterceptorType) => commandInterceptorGoingToRun(logger, commandInterceptorType, null);
        public static void CommandInterceptorRaisedException(ILogger logger, Type commandInterceptorType) => commandInterceptorRaisedException(logger, commandInterceptorType, null);
        public static void CommandInitialValidationResult(ILogger logger, string eventName, bool isValid) => commandInitialValidationResult(logger, eventName, isValid, null);
        public static void NotifyingCommandResultReactors(ILogger logger) => notifyingCommandResultReactors(logger, null);
        public static void ReturningFromDispatcher(ILogger logger, bool status) => returningFromDispatcher(logger, status, null);

        public static void QueryParameterNull(ILogger logger) => queryParameterNull(logger, null);
        public static void EnteredQueryDispatcher(ILogger logger, string correlationId, string queryName, Type queryType, Type queryResultType) => enteredQueryDispatcher(logger, correlationId, queryName, queryType, queryResultType, null);
        public static void QueryHandlerNotFound(ILogger logger, string queryName, Type queryType, Type queryResultType) => queryHandlerNotFound(logger, queryName, queryType, queryResultType, null);
        public static void QueryHandlerFound(ILogger logger, Type queryHandlerType, Type queryResultType) => queryHandlerFound(logger, queryHandlerType, queryResultType, null);
        public static void QueryForHandlerNotFound(ILogger logger, string queryName, Type queryType, Type queryResultType, string[] addresses) => queryForHandlerNotFound(logger, queryName, queryType, queryResultType, addresses, null);

        public static void MessageParameterNull(ILogger logger) => messageParameterNull(logger, null);
        public static void EnteredMessageDispatcher(ILogger logger, string correlationId, string messageName, Type messageType) => enteredMessageDispatcher(logger, correlationId, messageName, messageType, null);
        public static void MessageHandlersFound(ILogger logger, int messageHandlerCount) => messageHandlersFound(logger, messageHandlerCount, null);
        public static void MessageForHandlersNotFound(ILogger logger, string messageName, Type messageType, string[] addresses) => messageForHandlersNotFound(logger, messageName, messageType, addresses, null);

        #endregion Methods
    }
}
