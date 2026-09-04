namespace Innovation.ServiceBus.InProcess.vNext.Dispatching
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
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

        private IServiceScope serviceScope;

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

            this.serviceScope = serviceScopeFactory.CreateScope();
        }

        #endregion Constructor

        #region Properties

        public IDispatcherContext Context { get; private set; }
        public string CorrelationId { get; private set; } = Guid.NewGuid().ToString();

        #endregion Properties

        #region Methods

        public void SetCorrelationId([DisallowNull] string correlationId)
        {
            if (!string.IsNullOrWhiteSpace(value: correlationId))
            {
                this.CorrelationId = correlationId;
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
            var stopWatch = ValueStopwatch.StartNew();

            try
            {
                if (command == null)
                {
                    DispatcherLogging.CommandParameterNull(logger: this.logger);
                    throw new ArgumentNullException(paramName: nameof(command));
                }

                var commandType = command.GetType();

                DispatcherLogging.EnteredCommandDispatcher(
                    logger: this.logger, 
                    correlationId: this.CorrelationId, 
                    commandName: command.EventName, 
                    commandType: commandType);

                DispatcherLogging.CommandDetail(logger: this.logger, command: command);

                var commandBitsForCommandType = this.innovationRuntime.GetCommandBits(commandType: commandType);

                if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.CommandHandlerRegistered)) <= 0)
                {
                    // TODO: Ensure the runtime logs this as an error
                    // The runtime should check and ensure all commands have handlers registered
                    DispatcherLogging.CommandHandlerNotFound(logger: this.logger, eventName: command.EventName, commandType: commandType);

                    throw new CommandHandlerNotFoundException(command: command);
                }

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

                    if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.ContextAware)) != 0)
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

                DispatcherLogging.CommandHandlerFound(logger: this.logger, commandHandler.GetType());

                if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.CorrelationIdAware)) != 0)
                {
                    var correlationAwareCommand = command as ICorrelationAware;
                    correlationAwareCommand.CorrelationId = this.CorrelationId;
                }

                // If the command has reactors registered, queue them to run safely in the background -
                // never inline here, and never resolved from this Dispatcher's own scope, since that scope
                // may be disposed (e.g. an ASP.NET Core request scope) before the reactor gets a chance to run.
                if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.CommandReactor)) != 0)
                {
                    DispatcherLogging.NotifyingCommandReactors(logger: this.logger);

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
                        DispatcherLogging.CommandInterceptorsFound(logger: this.logger, commandInterceptorCount: commandInterceptors.Length);

                        foreach (var commandInterceptor in commandInterceptors)
                        {
                            // Using this nested try / catch block to avoid interceptor exceptions breaking the pipeline
                            try
                            {
                                DispatcherLogging.CommandInterceptorGoingToRun(logger: this.logger, commandInterceptorType: commandInterceptor.GetType());

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

                ICommandResult commandResult = null;
                IValidationResult validationResult = null;
                AggregateValidationResult aggregateValidationResult = null;
                var aggregateValidationErrors = this.innovationOptions.AggregateValidationErrors;

                // This bit is only set when the global validation option is enabled AND the command type actually
                // has something for DataAnnotations/MiniValidation to check (see InnovationRuntime). It must not be
                // used to decide whether registered IValidator<TCommand> instances run below - those are an
                // Innovation-library specific concern, gated purely by the CommandValidator bit.
                if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.IsValidationEnabled)) != 0)
                {
                    var dataAnnotationsValidator = new DataAnnotationsValidator(serviceProvider: this.serviceScope.ServiceProvider);

                    var validatorResult = await dataAnnotationsValidator.TryValidateObjectRecursive(target: command);

                    DispatcherLogging.CommandInitialValidationResult(logger: this.logger, eventName: command.EventName, isValid: validatorResult.isValid);

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

                    if (commandValidators != null)
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
                if ((commandBitsForCommandType & (1 << (int)CommandBitTypes.CommandResultReactor)) != 0)
                {
                    DispatcherLogging.NotifyingCommandResultReactors(logger: this.logger);

                    await this.reactorWorkQueue.EnqueueAsync(item: new ReactorWorkItem(
                        correlationId: this.CorrelationId,
                        reactorContractType: typeof(ICommandResultReactor<TCommand>),
                        command: command,
                        commandResult: finalResult));
                }

                DispatcherLogging.ReturningFromDispatcher(this.logger, finalResult.Success);

                if (this.innovationRuntime.HasAuditStoreRegistered(this.serviceScope.ServiceProvider))
                {
                    var auditStore = this.serviceScope.ServiceProvider.GetService<IAuditStore>();

                    if (auditStore != null)
                    {
                        DispatcherLogging.AuditStoreFound(logger: this.logger, auditStoreType: auditStore.GetType());
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
            var stopWatch = ValueStopwatch.StartNew();

            try
            {
                if (message == null)
                {
                    DispatcherLogging.MessageParameterNull(logger: this.logger);
                    throw new ArgumentNullException(paramName: nameof(message));
                }

                DispatcherLogging.EnteredMessageDispatcher(
                    logger: this.logger,
                    correlationId: this.CorrelationId,
                    messageName: message.EventName,
                    messageType: message.GetType());

                var auditStore = this.serviceScope.ServiceProvider.GetService<IAuditStore>();

                if (auditStore != null)
                {
                    DispatcherLogging.AuditStoreFound(logger: this.logger, auditStoreType: auditStore.GetType());
                }

                var handlers = this.ResolveMessageHandlers<TMessage>();

                DispatcherLogging.MessageHandlersFound(logger: this.logger, messageHandlerCount: handlers.Length);

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
            var stopWatch = ValueStopwatch.StartNew();

            try
            {
                if (message == null)
                {
                    DispatcherLogging.MessageParameterNull(logger: this.logger);
                    throw new ArgumentNullException(paramName: nameof(message));
                }

                DispatcherLogging.EnteredMessageDispatcher(
                    logger: this.logger,
                    correlationId: this.CorrelationId,
                    messageName: message.EventName,
                    messageType: message.GetType());

                var auditStore = this.serviceScope.ServiceProvider.GetService<IAuditStore>();

                if (auditStore != null)
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

                DispatcherLogging.MessageHandlersFound(logger: this.logger, messageHandlerCount: addressableHandlers.Length);

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
            var stopWatch = ValueStopwatch.StartNew();

            try
            {
                if (query == null)
                {
                    DispatcherLogging.QueryParameterNull(logger: this.logger);

                    throw new ArgumentNullException(paramName: nameof(query));
                }

                var auditStore = this.serviceScope.ServiceProvider.GetService<IAuditStore>();

                if (auditStore != null)
                {
                    DispatcherLogging.AuditStoreFound(logger: this.logger, auditStoreType: auditStore.GetType());
                }

                DispatcherLogging.EnteredQueryDispatcher(
                    logger: this.logger,
                    correlationId: this.CorrelationId,
                    queryName: query.EventName,
                    queryType: query.GetType(),
                    queryResultType: typeof(TQueryResult));

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

                DispatcherLogging.QueryHandlerFound(
                    logger: this.logger,
                    queryHandlerType: queryHandler.GetType(),
                    queryResultType: typeof(TQueryResult));

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
            var stopWatch = ValueStopwatch.StartNew();

            try
            {
                if (query == null)
                {
                    DispatcherLogging.QueryParameterNull(logger: this.logger);

                    throw new ArgumentNullException(paramName: nameof(query));
                }

                DispatcherLogging.EnteredQueryDispatcher(
                    logger: this.logger,
                    correlationId: this.CorrelationId,
                    queryName: query.EventName,
                    queryType: query.GetType(),
                    queryResultType: typeof(TQueryResult));

                var auditStore = this.serviceScope.ServiceProvider.GetService<IAuditStore>();

                if (auditStore != null)
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

                DispatcherLogging.QueryHandlerFound(
                    logger: this.logger,
                    queryHandlerType: queryHandler.GetType(),
                    queryResultType: typeof(TQueryResult));

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
            var commandInterceptors = this.serviceScope.ServiceProvider.GetServices<ICommandInterceptor<TCommand>>();

            return commandInterceptors.ToArray();
        }

        private IMessageHandler<TMessage>[] ResolveMessageHandlers<TMessage>() where TMessage : IMessage
        {
            var commandHandlers = this.serviceScope.ServiceProvider.GetServices<IMessageHandler<TMessage>>();

            return commandHandlers.ToArray();
        }

        private IValidator<TCommand>[] ResolveCommandValidators<TCommand>() where TCommand : ICommand
        {
            var commandValidators = this.serviceScope.ServiceProvider.GetServices<IValidator<TCommand>>();

            return commandValidators.ToArray();
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
            var queryHandler = this.serviceScope.ServiceProvider.GetServices<IQueryHandler<TQuery, TQueryResult>>();

            return queryHandler.ToArray();
        }

        #endregion Private Methods
    }
}