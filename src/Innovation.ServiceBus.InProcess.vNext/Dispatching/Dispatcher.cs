namespace Innovation.ServiceBus.InProcess.vNext.Dispatching
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;

    using Settings;
    using Validators;
    using Exceptions;
    using Api.vNext.Core;
    using Api.vNext.Querying;
    using Api.vNext.Reactions;
    using Api.vNext.Messaging;
    using Api.vNext.Commanding;
    using Api.vNext.Validation;
    using Api.vNext.Dispatching;
    using Api.vNext.Interceptors;
    using Api.vNext.CommandHelpers;

    /// <summary>
    /// This is the implementation of the dispatcher for the in process service bus.
    /// </summary>
    public class Dispatcher : IDispatcher, IDisposable
    {
        #region Fields

        private readonly ILogger logger;
        private readonly InnovationOptions innovationOptions;
        private readonly InnovationRuntime innovationRuntime;
        private readonly IReactorWorkQueue reactorWorkQueue;

        // Read once in the constructor rather than through IOptions on every dispatch - InnovationOptions is
        // registered via Configure() and never mutated after the container is built, so the value is stable
        // for the lifetime of this Dispatcher and the repeated property indirection is pure overhead.
        private readonly bool aggregateValidationErrors;

        // Whether any IAuditStore is registered, resolved once here rather than per dispatch. InnovationRuntime
        // already memoizes the answer, but every dispatch method needs it, so holding it as a field turns a call
        // plus a nullable-bool check into a plain field read on each of the six dispatch entry points.
        private readonly bool hasAuditStoreRegistered;

        private IServiceScope serviceScope;

        // this.serviceScope.ServiceProvider never changes for the lifetime of this Dispatcher, and every dispatch
        // reads it at least once (often two or three times), so it is cached here to avoid repeating the interface
        // property call on the hot path.
        private readonly IServiceProvider serviceProvider;

        // Backing field for the lazily generated CorrelationId - see the CorrelationId property.
        private string correlationId;

        // Combines every CommandBitTypes flag that represents an optional, cross-cutting pipeline
        // feature (i.e. everything except CommandHandlerRegistered/ContextAware, which are handled
        // separately). When none of these bits are set for a command type, the whole block of
        // individual per-feature checks in Command<TCommand> can be skipped with a single test.
        private const int CrossCuttingFeaturesMask =
            (1 << (int)CommandBitTypes.CommandValidator) |
            (1 << (int)CommandBitTypes.CommandReactor) |
            (1 << (int)CommandBitTypes.CommandResultReactor) |
            (1 << (int)CommandBitTypes.CommandInterceptor) |
            (1 << (int)CommandBitTypes.CorrelationIdAware) |
            (1 << (int)CommandBitTypes.IsValidationEnabled);

        #endregion Fields

        #region Constructor

        public Dispatcher(
            ILogger<Dispatcher> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<InnovationOptions> innovationOptionsOptions,
            InnovationRuntime innovationRuntime,
            IReactorWorkQueue reactorWorkQueue)
        {
            this.logger = logger;
            this.innovationOptions = innovationOptionsOptions.Value;
            this.innovationRuntime = innovationRuntime;
            this.reactorWorkQueue = reactorWorkQueue;
            this.aggregateValidationErrors = this.innovationOptions.AggregateValidationErrors;

            this.serviceScope = serviceScopeFactory.CreateScope();
            this.serviceProvider = this.serviceScope.ServiceProvider;
            this.hasAuditStoreRegistered = innovationRuntime.HasAuditStoreRegistered(this.serviceProvider);
        }

        #endregion Constructor

        #region Properties

        public IDispatcherContext Context { get; private set; }

        // Generated on first read instead of in the constructor. Dispatcher is registered Transient, so an
        // eagerly generated id cost a Guid.NewGuid() plus a 36 char string allocation for every resolution -
        // including the (common) case where nothing on the dispatch path ever asks for it, and the case where
        // the caller immediately overwrites it via SetCorrelationId. The value is still generated at most once
        // and remains stable for the lifetime of this Dispatcher, so callers observe no behavioral difference.
        public string CorrelationId => this.correlationId ??= Guid.NewGuid().ToString();

        #endregion Properties

        #region Methods

        public void SetCorrelationId([DisallowNull] string correlationId)
        {
            if (!string.IsNullOrWhiteSpace(value: correlationId))
            {
                this.correlationId = correlationId;
            }
        }

        public void SetContext([DisallowNull] IDispatcherContext dispatcherContext)
        {
            this.Context = dispatcherContext ?? throw new ArgumentNullException(paramName: nameof(dispatcherContext));
            this.Context.SetCorrelationId(correlationId: this.CorrelationId);
        }

        // Dispatcher is registered Transient (see ServiceCollectionExtensions), and the constructor eagerly
        // creates its own IServiceScope so resolutions made through this.serviceScope never depend on the
        // caller's ambient scope surviving. Because the type is Transient, the DI container automatically
        // tracks and disposes any instance it creates that implements IDisposable when the scope that created
        // it is disposed - so implementing Dispose here (rather than requiring callers to do so) is enough to
        // release this.serviceScope, and everything resolved through it, without any behavioral change for
        // existing callers.
        public void Dispose()
        {
            this.serviceScope?.Dispose();
        }

        public async ValueTask<ICommandResult> Command<TCommand>([DisallowNull] TCommand command, bool suppressExceptions = true) where TCommand : ICommand
        {
            // Audit-store presence is memoized after the first call (see InnovationRuntime.HasAuditStoreRegistered),
            // so checking it up front lets us skip starting the stopwatch entirely when there is no audit store to time for.
            var hasAuditStoreRegistered = this.innovationRuntime.HasAuditStoreRegistered(this.serviceScope.ServiceProvider);
            var stopWatch = hasAuditStoreRegistered ? ValueStopwatch.StartNew() : default;

            try
            {
                if (command == null)
                {
                    DispatcherLogging.CommandParameterNull(logger: this.logger);
                    throw new ArgumentNullException(paramName: nameof(command));
                }

                var commandType = command.GetType();

                // Every DispatcherLogging.* call below is a LoggerMessage.Define delegate that performs its own
                // ILogger.IsEnabled check, and ILogger.IsEnabled walks the whole provider/filter chain each time.
                // A single command dispatch fires four to eight of those, so the check is hoisted here and the
                // call sites are guarded, turning N chain walks per dispatch into one.
                // The Trace-level CommandDetail log is nested inside this Debug check: Trace is a lower level than
                // Debug, so any ILogger whose IsEnabled honors level ordering (the built-in one, and every provider
                // shipped with Microsoft.Extensions.Logging) cannot have Trace enabled while Debug is disabled.
                var isDebugEnabled = this.logger.IsEnabled(logLevel: LogLevel.Debug);

                if (isDebugEnabled)
                {
                    DispatcherLogging.EnteredCommandDispatcher(
                        logger: this.logger,
                        correlationId: this.CorrelationId,
                        commandName: command.EventName,
                        commandType: commandType);

                    DispatcherLogging.CommandDetail(logger: this.logger, command: command);
                }

                var commandBitsForCommandType = this.innovationRuntime.GetCommandBits(commandType: commandType);

                if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.CommandHandlerRegistered)) <= 0)
                {
                    // TODO: Ensure the runtime logs this as an error
                    // The runtime should check and ensure all commands have handlers registered
                    DispatcherLogging.CommandHandlerNotFound(logger: this.logger, eventName: command.EventName, commandType: commandType);

                    throw new CommandHandlerNotFoundException(command: command);
                }

                // Only commands that actually implement IContextAware (tracked by the ContextAware bit)
                if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.ContextAware)) != 0)
                {
                    if (this.Context == null)
                    {
                        DispatcherLogging.ContextNotSet(
                            logger: this.logger,
                            eventName: command.EventName,
                            correlationId: this.CorrelationId,
                            eventType: commandType);
                    }
                    else
                    {
                        var contextAwareCommand = command as IContextAware;
                        contextAwareCommand.SetContext(dispatcherContext: this.Context);
                    }
                }

                var commandHandler = this.serviceScope.ServiceProvider.GetService<ICommandHandler<TCommand>>();

                // Just because there is a command handler has been registered, it does not mean we can get an instance.
                // If it is missing dependencies for example, it cannot be instantiated, though this normally throws an error
                // lets not make assumptions and be safe
                if (commandHandler == null)
                {
                    DispatcherLogging.CommandHandlerNotFound(logger: this.logger, eventName: command.EventName, commandType: commandType);

                    throw new CommandHandlerNotFoundException(command: command);
                }

                if (isDebugEnabled)
                {
                    DispatcherLogging.CommandHandlerFound(logger: this.logger, commandHandler.GetType());
                }

                // Single combined check covering CorrelationIdAware/CommandReactor/CommandInterceptor/
                // IsValidationEnabled/CommandValidator/CommandResultReactor. When a command type uses none
                // of these optional pipeline features, this lets the fast path skip straight to invoking
                // the handler below instead of evaluating six separate bitmask checks.
                var hasCrossCuttingFeatures = (commandBitsForCommandType & CrossCuttingFeaturesMask) != 0;

                ICommandResult commandResult = null;
                IValidationResult validationResult = null;
                AggregateValidationResult aggregateValidationResult = null;
                var aggregateValidationErrors = this.aggregateValidationErrors;

                if (hasCrossCuttingFeatures)
                {
                    // Set on the handler rather than on the command, which is what the Query pipeline has
                    // always done.
                    if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.CorrelationIdAware)) != 0
                        && commandHandler is ICorrelationAware correlationAwareCommandHandler)
                    {
                        correlationAwareCommandHandler.CorrelationId = this.CorrelationId;
                    }

                    // If the command has reactors registered, queue them to run safely in the background -
                    // never inline here, and never resolved from this Dispatcher's own scope, since that scope
                    // may be disposed (e.g. an ASP.NET Core request scope) before the reactor gets a chance to run.
                    if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.CommandReactor)) != 0)
                    {
                        if (isDebugEnabled)
                        {
                            DispatcherLogging.NotifyingCommandReactors(logger: this.logger);
                        }

                        await this.reactorWorkQueue.EnqueueAsync(item: new ReactorWorkItem(
                            correlationId: this.CorrelationId,
                            reactorContractType: typeof(ICommandReactor<TCommand>),
                            command: command,
                            commandResult: null));
                    }

                    // If the command has interceptor registered, process them
                    if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.CommandInterceptor)) != 0)
                    {
                        var commandInterceptors = this.ResolveCommandInterceptors<TCommand>();

                        if (commandInterceptors != null && commandInterceptors.Length > 0)
                        {
                            if (isDebugEnabled)
                            {
                                DispatcherLogging.CommandInterceptorsFound(logger: this.logger, commandInterceptorCount: commandInterceptors.Length);
                            }

                            foreach (var commandInterceptor in commandInterceptors)
                            {
                                // Using this nested try / catch block to avoid interceptor exceptions breaking the pipeline
                                try
                                {
                                    if (isDebugEnabled)
                                    {
                                        DispatcherLogging.CommandInterceptorGoingToRun(logger: this.logger, commandInterceptorType: commandInterceptor.GetType());
                                    }

                                    await commandInterceptor.Intercept(command: command);
                                }
                                catch (Exception ex)
                                {
                                    DispatcherLogging.CommandInterceptorRaisedException(logger: this.logger, commandInterceptorType: commandInterceptor.GetType());
                                    this.logger.LogError(exception: ex, message: ex.GetInnerMostMessage());
                                }
                            }
                        }
                    }

                    // This bit is only set when the global validation option is enabled AND the command type actually
                    // has something for DataAnnotations/MiniValidation to check (see InnovationRuntime). It must not be
                    // used to decide whether registered IValidator<TCommand> instances run below - those are an
                    // Innovation-library specific concern, gated purely by the CommandValidator bit.
                    if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.IsValidationEnabled)) != 0)
                    {
                        var dataAnnotationsValidator = new DataAnnotationsValidator(serviceProvider: this.serviceScope.ServiceProvider);

                        var validatorResult = await dataAnnotationsValidator.TryValidateObjectRecursive(target: command);

                        if (isDebugEnabled)
                        {
                            DispatcherLogging.CommandInitialValidationResult(logger: this.logger, eventName: command.EventName, isValid: validatorResult.isValid);
                        }

                        if (!validatorResult.isValid && validatorResult.Errors?.Count > 0)
                        {
                            if (aggregateValidationErrors)
                            {
                                aggregateValidationResult ??= new AggregateValidationResult();
                                aggregateValidationResult.Merge(errorsToMerge: validatorResult.Errors);
                            }
                            else
                            {
                                commandResult = new CommandResult(errors: validatorResult.Errors);
                            }
                        }
                    }

                    // Custom validators always run when registered, regardless of whether global DataAnnotations
                    // validation is enabled, was required for this command type, or already failed above. This must
                    // NOT be conditioned on commandResult (the DataAnnotations outcome) - that was the original bug.
                    if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.CommandValidator)) != 0)
                    {
                        var commandValidators = this.ResolveCommandValidators<TCommand>();

                        if (isDebugEnabled && commandValidators != null)
                        {
                            DispatcherLogging.CommandValidatorsFound(logger: this.logger, commandValidatorCount: commandValidators.Length);
                        }

                        if (commandValidators != null && commandValidators.Length > 0)
                        {
                            foreach (var commandValidator in commandValidators)
                            {
                                var intermediateValidationResult = await commandValidator.Validate(command: command);

                                if (!intermediateValidationResult.Success)
                                {
                                    if (aggregateValidationErrors)
                                    {
                                        aggregateValidationResult ??= new AggregateValidationResult();

                                        var validatorErrors = intermediateValidationResult.Errors;

                                        if (validatorErrors != null && validatorErrors.Count > 0)
                                        {
                                            aggregateValidationResult.Merge(errorsToMerge: validatorErrors);
                                        }
                                        else
                                        {
                                            aggregateValidationResult.MergeFallback(validatorTypeName: commandValidator.GetType().Name);
                                        }
                                    }
                                    else
                                    {
                                        validationResult = intermediateValidationResult;

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                ICommandResult finalResult;

                if (aggregateValidationErrors)
                {
                    finalResult = aggregateValidationResult != null && !aggregateValidationResult.Success
                        ? aggregateValidationResult
                        : await commandHandler.Handle(command: command);
                }
                else
                {
                    finalResult = validationResult ?? (commandResult == null ? await commandHandler.Handle(command: command) : commandResult.Success ? await commandHandler.Handle(command: command) : commandResult);
                }

                // If the command has result reactors registered, queue them to run safely in the background
                if (hasCrossCuttingFeatures && (commandBitsForCommandType & (1 << (int)CommandBitTypes.CommandResultReactor)) != 0)
                {
                    if (isDebugEnabled)
                    {
                        DispatcherLogging.NotifyingCommandResultReactors(logger: this.logger);
                    }

                    await this.reactorWorkQueue.EnqueueAsync(item: new ReactorWorkItem(
                        correlationId: this.CorrelationId,
                        reactorContractType: typeof(ICommandResultReactor<TCommand>),
                        command: command,
                        commandResult: finalResult));
                }

                if (isDebugEnabled)
                {
                    DispatcherLogging.ReturningFromDispatcher(this.logger, finalResult.Success);
                }

                if (hasAuditStoreRegistered)
                {
                    var auditStore = this.serviceScope.ServiceProvider.GetService<IAuditStore>();

                    if (auditStore != null)
                    {
                        if (isDebugEnabled)
                        {
                            DispatcherLogging.AuditStoreFound(logger: this.logger, auditStoreType: auditStore.GetType());
                        }

                        await auditStore.Log(
                            auditContext: new AuditContext(correlationId: this.CorrelationId, runtimeMilliSeconds: (long)stopWatch.GetElapsedTime().TotalMilliseconds),
                            command: command,
                            commandResult: finalResult);
                    }
                }

                return finalResult;
            }
            catch (Exception ex)
            {
                this.logger.LogError(exception: ex, message: ex.GetInnerMostMessage());

                if (suppressExceptions)
                {
                    return this.CreateFromException(ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public async ValueTask Message<TMessage>([DisallowNull] TMessage message) where TMessage : IMessage
        {
            // Same memoized fast path Command uses: when no IAuditStore is registered there is nothing to time,
            // so neither the stopwatch nor the per-dispatch IAuditStore resolution needs to happen at all.
            var hasAuditStoreRegistered = this.innovationRuntime.HasAuditStoreRegistered(this.serviceScope.ServiceProvider);
            var stopWatch = hasAuditStoreRegistered ? ValueStopwatch.StartNew() : default;

            try
            {
                if (message == null)
                {
                    DispatcherLogging.MessageParameterNull(logger: this.logger);
                    throw new ArgumentNullException(paramName: nameof(message));
                }

                // Hoisted for the same reason as in Command - see the comment there.
                var isDebugEnabled = this.logger.IsEnabled(logLevel: LogLevel.Debug);

                if (isDebugEnabled)
                {
                    DispatcherLogging.EnteredMessageDispatcher(
                        logger: this.logger,
                        correlationId: this.CorrelationId,
                        messageName: message.EventName,
                        messageType: message.GetType());
                }

                var auditStore = hasAuditStoreRegistered ? this.serviceScope.ServiceProvider.GetService<IAuditStore>() : null;

                if (auditStore != null && isDebugEnabled)
                {
                    DispatcherLogging.AuditStoreFound(logger: this.logger, auditStoreType: auditStore.GetType());
                }

                var handlers = this.ResolveMessageHandlers<TMessage>();

                if (isDebugEnabled)
                {
                    DispatcherLogging.MessageHandlersFound(logger: this.logger, messageHandlerCount: handlers.Length);
                }

                if (auditStore != null)
                {
                    await auditStore.Log(auditContext: new AuditContext(correlationId: this.CorrelationId, runtimeMilliSeconds: (long)stopWatch.GetElapsedTime().TotalMilliseconds), message: message);
                }

                foreach (var handler in handlers)
                {
                    await handler.Handle(message: message);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(exception: ex, message: ex.GetInnerMostMessage());

                throw;
            }
        }

        public async ValueTask MessageFor<TMessage>([DisallowNull] TMessage message, [DisallowNull] params string[] addresses) where TMessage : IMessage
        {
            var hasAuditStoreRegistered = this.innovationRuntime.HasAuditStoreRegistered(this.serviceScope.ServiceProvider);
            var stopWatch = hasAuditStoreRegistered ? ValueStopwatch.StartNew() : default;

            try
            {
                if (message == null)
                {
                    DispatcherLogging.MessageParameterNull(logger: this.logger);
                    throw new ArgumentNullException(paramName: nameof(message));
                }

                var isDebugEnabled = this.logger.IsEnabled(logLevel: LogLevel.Debug);

                if (isDebugEnabled)
                {
                    DispatcherLogging.EnteredMessageDispatcher(
                        logger: this.logger,
                        correlationId: this.CorrelationId,
                        messageName: message.EventName,
                        messageType: message.GetType());
                }

                var auditStore = hasAuditStoreRegistered ? this.serviceScope.ServiceProvider.GetService<IAuditStore>() : null;

                if (auditStore != null && isDebugEnabled)
                {
                    DispatcherLogging.AuditStoreFound(logger: this.logger, auditStoreType: auditStore.GetType());
                }

                var handlers = this.ResolveMessageHandlers<TMessage>();

                var addressableHandlers = handlers.OfType<IAddressable>().Where(x => x.Handles.Intersect(addresses).Any()).Cast<IMessageHandler<TMessage>>().ToArray();

                if (addressableHandlers.Length == 0)
                {
                    DispatcherLogging.MessageForHandlersNotFound(logger: this.logger, messageName: message.EventName, messageType: message.GetType(), addresses: addresses);
                    return;
                }

                if (isDebugEnabled)
                {
                    DispatcherLogging.MessageHandlersFound(logger: this.logger, messageHandlerCount: addressableHandlers.Length);
                }

                if (auditStore != null)
                {
                    await auditStore.Log(auditContext: new AuditContext(correlationId: this.CorrelationId, runtimeMilliSeconds: (long)stopWatch.GetElapsedTime().TotalMilliseconds), message: message);
                }

                foreach (var handler in addressableHandlers)
                {
                    await handler.Handle(message: message);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(exception: ex, message: ex.GetInnerMostMessage());

                throw;
            }
        }

        public async ValueTask<TQueryResult> Query<TQuery, TQueryResult>([DisallowNull] TQuery query) where TQuery : IQuery where TQueryResult : IQueryResult
        {
            // Same memoized fast path Command uses - previously this method resolved IAuditStore from DI and
            // started a stopwatch on every single dispatch, even for the (overwhelmingly common) case where no
            // audit store is registered at all.
            var hasAuditStoreRegistered = this.innovationRuntime.HasAuditStoreRegistered(this.serviceScope.ServiceProvider);
            var stopWatch = hasAuditStoreRegistered ? ValueStopwatch.StartNew() : default;

            try
            {
                if (query == null)
                {
                    DispatcherLogging.QueryParameterNull(logger: this.logger);

                    throw new ArgumentNullException(paramName: nameof(query));
                }

                var auditStore = hasAuditStoreRegistered ? this.serviceScope.ServiceProvider.GetService<IAuditStore>() : null;

                // Hoisted for the same reason as in Command - see the comment there.
                var isDebugEnabled = this.logger.IsEnabled(logLevel: LogLevel.Debug);

                if (auditStore != null && isDebugEnabled)
                {
                    DispatcherLogging.AuditStoreFound(logger: this.logger, auditStoreType: auditStore.GetType());
                }

                if (isDebugEnabled)
                {
                    DispatcherLogging.EnteredQueryDispatcher(
                        logger: this.logger,
                        correlationId: this.CorrelationId,
                        queryName: query.EventName,
                        queryType: query.GetType(),
                        queryResultType: typeof(TQueryResult));
                }

                var queryHandler = this.Resolve<TQuery, TQueryResult>();

                if (queryHandler == null)
                {
                    DispatcherLogging.QueryHandlerNotFound(
                        logger: this.logger,
                        queryName: query.EventName,
                        queryType: query.GetType(),
                        queryResultType: typeof(TQueryResult));

                    throw new QueryHandlerNotFoundException(query);
                }

                if (query is IContextAware contextAwareQuery)
                {
                    if (this.Context == null)
                    {
                        DispatcherLogging.ContextNotSet(
                            logger: this.logger,
                            eventName: query.EventName,
                            correlationId: this.CorrelationId,
                            eventType: query.GetType());
                    }
                    else
                    {
                        contextAwareQuery.SetContext(dispatcherContext: this.Context);
                    }
                }

                if (queryHandler is ICorrelationAware correlationAwareQueryHandler)
                {
                    correlationAwareQueryHandler.CorrelationId = this.CorrelationId;
                }

                if (isDebugEnabled)
                {
                    DispatcherLogging.QueryHandlerFound(
                        logger: this.logger,
                        queryHandlerType: queryHandler.GetType(),
                        queryResultType: typeof(TQueryResult));
                }

                if (auditStore != null)
                {
                    await auditStore.Log(auditContext: new AuditContext(correlationId: this.CorrelationId, runtimeMilliSeconds: (long)stopWatch.GetElapsedTime().TotalMilliseconds), query: query);
                }

                return await queryHandler.Handle(query);
            }
            catch (Exception ex)
            {
                this.logger.LogError(exception: ex, message: ex.GetInnerMostMessage());

                throw;
            }
        }

        public async ValueTask<TQueryResult> QueryFor<TQuery, TQueryResult>([DisallowNull] TQuery query, [DisallowNull] params string[] addresses) where TQuery : IQuery where TQueryResult : IQueryResult
        {
            var hasAuditStoreRegistered = this.innovationRuntime.HasAuditStoreRegistered(this.serviceScope.ServiceProvider);
            var stopWatch = hasAuditStoreRegistered ? ValueStopwatch.StartNew() : default;

            try
            {
                if (query == null)
                {
                    DispatcherLogging.QueryParameterNull(logger: this.logger);

                    throw new ArgumentNullException(paramName: nameof(query));
                }

                var isDebugEnabled = this.logger.IsEnabled(logLevel: LogLevel.Debug);

                if (isDebugEnabled)
                {
                    DispatcherLogging.EnteredQueryDispatcher(
                        logger: this.logger,
                        correlationId: this.CorrelationId,
                        queryName: query.EventName,
                        queryType: query.GetType(),
                        queryResultType: typeof(TQueryResult));
                }

                var auditStore = hasAuditStoreRegistered ? this.serviceScope.ServiceProvider.GetService<IAuditStore>() : null;

                if (auditStore != null && isDebugEnabled)
                {
                    DispatcherLogging.AuditStoreFound(logger: this.logger, auditStoreType: auditStore.GetType());
                }

                var queryHandlers = this.ResolveAll<TQuery, TQueryResult>();

                var addressableHandlers = queryHandlers.OfType<IAddressable>().Where(x => x.Handles.Intersect(addresses).Any()).Cast<IQueryHandler<TQuery, TQueryResult>>().ToArray();

                if (addressableHandlers.Length == 0)
                {
                    DispatcherLogging.QueryForHandlerNotFound(
                        logger: this.logger,
                        queryName: query.EventName,
                        queryType: query.GetType(),
                        queryResultType: typeof(TQueryResult),
                        addresses: addresses);

                    throw new QueryHandlerNotFoundException(query);
                }

                var queryHandler = addressableHandlers[0];

                if (isDebugEnabled)
                {
                    DispatcherLogging.QueryHandlerFound(
                        logger: this.logger,
                        queryHandlerType: queryHandler.GetType(),
                        queryResultType: typeof(TQueryResult));
                }

                if (auditStore != null)
                {
                    await auditStore.Log(auditContext: new AuditContext(correlationId: this.CorrelationId, runtimeMilliSeconds: (long)stopWatch.GetElapsedTime().TotalMilliseconds), query: query);
                }

                return await queryHandler.Handle(query);
            }
            catch (Exception ex)
            {
                this.logger.LogError(exception: ex, message: ex.GetInnerMostMessage());

                throw;
            }
        }

        #endregion Methods

        #region Private Methods

        private ICommandResult CreateFromException([DisallowNull] Exception ex)
        {
            var exceptionResult = new CommandExceptionResult(message: ex.Message, exception: ex);

            return exceptionResult;
        }

        private ICommandInterceptor<TCommand>[] ResolveCommandInterceptors<TCommand>() where TCommand : ICommand
        {
            return this.ResolveMany<ICommandInterceptor<TCommand>>();
        }

        private IMessageHandler<TMessage>[] ResolveMessageHandlers<TMessage>() where TMessage : IMessage
        {
            return this.ResolveMany<IMessageHandler<TMessage>>();
        }

        private IValidator<TCommand>[] ResolveCommandValidators<TCommand>() where TCommand : ICommand
        {
            return this.ResolveMany<IValidator<TCommand>>();
        }

        // GetServices<T>() is declared as returning IEnumerable<T>, but Microsoft.Extensions.DependencyInjection
        // always materializes a T[] to satisfy an IEnumerable<T> request. Calling .ToArray() on that therefore
        // allocated a second array plus an enumerator on every dispatch, purely to copy an array that already
        // existed. Casting instead removes both allocations, with a ToArray() fallback so a custom
        // IServiceProvider that returns some other IEnumerable<T> still behaves exactly as before.
        private T[] ResolveMany<T>()
        {
            var services = this.serviceScope.ServiceProvider.GetService<IEnumerable<T>>();

            return services switch
            {
                null => Array.Empty<T>(),
                T[] array => array,
                _ => services.ToArray()
            };
        }

        private IQueryHandler<TQuery, TQueryResult> Resolve<TQuery, TQueryResult>()
            where TQuery : IQuery
            where TQueryResult : IQueryResult
        {
            var queryHandler = this.serviceScope.ServiceProvider.GetService<IQueryHandler<TQuery, TQueryResult>>();

            return queryHandler;
        }

        private IQueryHandler<TQuery, TQueryResult>[] ResolveAll<TQuery, TQueryResult>()
            where TQuery : IQuery
            where TQueryResult : IQueryResult
        {
            return this.ResolveMany<IQueryHandler<TQuery, TQueryResult>>();
        }

        #endregion Private Methods
    }
}