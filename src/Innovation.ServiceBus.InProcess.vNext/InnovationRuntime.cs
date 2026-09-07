namespace Innovation.ServiceBus.InProcess.vNext
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Collections.Frozen;
    using System.Collections.Generic;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyModel;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using MiniValidation;

    using Settings;
    using Dispatching;
    using Exceptions;
    using Api.vNext.Core;
    using Api.vNext.Querying;
    using Api.vNext.Reactions;
    using Api.vNext.Messaging;
    using Api.vNext.Commanding;
    using Api.vNext.Validation;
    using Api.vNext.Dispatching;
    using Api.vNext.Interceptors;

    public class InnovationRuntime
    {
        #region Fields

        internal static HashSet<string> ReferenceAssemblies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Innovation.Api.vNext",
            "Innovation.ServiceBus.InProcess.vNext"
        };
        private readonly IServiceCollection services;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;

        private readonly bool isValidationEnabled;
        private readonly InnovationOptions innovationOptions;
        private readonly Dictionary<Type, int> commandLookup = new Dictionary<Type, int>();

        // Built once at the end of Configure(), from commandLookup, and used for every subsequent per-dispatch
        // lookup. commandLookup is only ever written to during assembly scanning, so once Configure() has run the
        // contents are fixed - a FrozenDictionary gives a faster read-only lookup for exactly that access pattern.
        // Stays null until Configure() completes so GetCommandBits keeps working if it is somehow called earlier.
        private FrozenDictionary<Type, int> frozenCommandLookup;

        // Every ICommand/IQuery type discovered while scanning assemblies, regardless of whether a handler
        // was found for it - used purely to validate, at startup, that every discovered command/query has a
        // handler registered somewhere. queriesWithHandlers mirrors commandLookup's CommandHandlerRegistered
        // bit, but for queries, which otherwise have no bitmask tracking at all.
        private readonly HashSet<Type> discoveredCommandTypes = new HashSet<Type>();
        private readonly HashSet<Type> discoveredQueryTypes = new HashSet<Type>();
        private readonly HashSet<Type> queriesWithHandlers = new HashSet<Type>();

        #endregion Fields

        #region Constructor

        public InnovationRuntime(
            IServiceCollection services,
            ILogger<InnovationRuntime> logger,
            IServiceProvider serviceProviderInstance,
            IOptions<InnovationOptions> innovationOptions)
        {
            this.services = services;
            this.logger = logger;
            this.serviceProvider = serviceProviderInstance;
            this.innovationOptions = innovationOptions.Value;
            this.isValidationEnabled = this.innovationOptions.IsValidationEnabled;
        }

        #endregion Constructor

        #region Methods

        // Computed lazily, on first use, against the caller's own live IServiceProvider (never this.serviceProvider,
        // which is a temporary snapshot taken inside AddInnovationvNext() before any .WithAuditStore<T>() builder
        // call has had a chance to run). By the time a Dispatcher is actually resolved and calls this, the
        // application's real DI container is guaranteed to be fully built, so the check is always accurate.
        // Concurrent first-calls may compute this more than once; the result is deterministic so that's harmless.
        private bool? hasAuditStoreRegistered;

        public bool HasAuditStoreRegistered(IServiceProvider callerServiceProvider)
        {
            return this.hasAuditStoreRegistered ??= callerServiceProvider.GetService<IAuditStore>() != null;
        }

        public int GetCommandBits(in Type commandType)
        {
            // TryGetValue rather than the plain indexer: a command type that genuinely has no handler
            // registered anywhere is never added to commandLookup (see RegisterHandlers), so falling back to
            // 0/Unknown here (instead of throwing KeyNotFoundException) lets Dispatcher.Command fall through
            // to its normal, friendly CommandHandlerNotFoundException path.
            var lookup = this.frozenCommandLookup;

            if (lookup != null)
            {
                return lookup.TryGetValue(key: commandType, value: out var frozenBits) ? frozenBits : 0;
            }

            return this.commandLookup.TryGetValue(key: commandType, value: out var bits) ? bits : 0;
        }

        public void Configure()
        {
            this.RegisterHandlers();
            this.LogCommandBits();
            this.ValidateHandlersRegistered();

            this.frozenCommandLookup = this.commandLookup.ToFrozenDictionary();
        }

        #endregion Methods

        #region Private Methods

        private void RegisterHandlers()
        {
            var assemblyDictionary = new Dictionary<string, Assembly>();

            var discoveredAssemblies = this.LoadReferencingLibraries();

            foreach (var discoveredAssembly in discoveredAssemblies)
            {
                var assemblyName = discoveredAssembly.GetName();

                if (ReferenceAssemblies.Contains(assemblyName.Name))
                {
                    continue;
                }

                if (!assemblyDictionary.ContainsKey(assemblyName.Name))
                {
                    assemblyDictionary.Add(assemblyName.Name, discoveredAssembly);
                }
            }

            if (!this.innovationOptions.DisableDynamicLoading)
            {
                var loadedAssemblies = this.LoadFromLocations();

                if (loadedAssemblies?.Length > 0)
                {
                    foreach (var loadedAssembly in loadedAssemblies)
                    {
                        assemblyDictionary.Add(loadedAssembly.GetName().Name, loadedAssembly);
                    }
                }
            }

            var assembliesToIgnore = new List<string>();

            foreach (var assembly in assemblyDictionary)
            {
                if (assembly.Key.StartsWith("Microsoft.") ||
                    assembly.Key.StartsWith("System.") ||
                    assembly.Key.StartsWith("Newtonsoft.") ||
                    assembly.Key.StartsWith("NuGet.") ||
                    assembly.Key.StartsWith("xunit.") ||
                    assembly.Key.StartsWith("Serilog.") ||
                    assembly.Key.StartsWith("Newtonsoft."))
                {
                    assembliesToIgnore.Add(assembly.Key);
                }
            }

            foreach (var ignoredAssembly in assembliesToIgnore)
            {
                if (assemblyDictionary.ContainsKey(ignoredAssembly))
                {
                    assemblyDictionary.Remove(ignoredAssembly);
                }
            }

            var finalAssemblyList = assemblyDictionary.Select(x => x.Value).ToArray();

            foreach (var assembly in finalAssemblyList)
            {
                this.logger.LogDebug("Processing Assembly: {Assembly}", assembly.GetName());

                TypeInfo[] types = null;

                try
                {
                    types = assembly.DefinedTypes.ToArray();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex.Message, ex);
                }

                if (types == null)
                {
                    this.logger.LogDebug("No Defined Types Found");

                    continue;
                }

                // Records every concrete ICommand/IQuery type in this assembly, whether or not it turns out to
                // have a handler - the handler-discovery loop below only ever looks at types with a generic
                // interface (handlers/reactors/interceptors/validators), so a command/query with no handler at
                // all would otherwise never be seen anywhere.
                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface)
                    {
                        continue;
                    }

                    var asType = type.AsType();

                    if (typeof(ICommand).IsAssignableFrom(asType))
                    {
                        this.discoveredCommandTypes.Add(asType);
                    }

                    if (typeof(IQuery).IsAssignableFrom(asType))
                    {
                        this.discoveredQueryTypes.Add(asType);
                    }
                }

                foreach (var type in types.Where(x => x.ImplementedInterfaces.Any(y => y.GenericTypeArguments.Any())))
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    var isCommandHandler = type.AsType().IsGenericTypeOf(typeof(ICommandHandler<ICommand>));
                    var isMessageHandler = type.AsType().IsGenericTypeOf(typeof(IMessageHandler<IMessage>));
                    var isQueryHandler = type.AsType().IsGenericTypeOf(typeof(IQueryHandler<IQuery, IQueryResult>));
                    var isCommandReactor = type.AsType().IsGenericTypeOf(typeof(ICommandReactor<ICommand>));
                    var isCommandResultReactor = type.AsType().IsGenericTypeOf(typeof(ICommandResultReactor<ICommand>));
                    var isCommandInterceptor = type.AsType().IsGenericTypeOf(typeof(ICommandInterceptor<ICommand>));
                    var isCommandValidator = type.AsType().IsGenericTypeOf(typeof(IValidator<ICommand>));

                    if (isCommandHandler)
                    {
                        var commandHandlerInterfaces = type.ImplementedInterfaces.Where(x => x.IsGenericTypeOf(typeof(ICommandHandler<ICommand>)));

                        this.logger.LogDebug("{TypeName} Contains {Count} Command Handler's", type.Name, commandHandlerInterfaces.Count());

                        foreach (var commandHandlerInterface in commandHandlerInterfaces)
                        {
                            var genericArguments = commandHandlerInterface.GetGenericArguments();

                            if (genericArguments != null && genericArguments.Count() == 1)
                            {
                                this.logger.LogDebug("{TypeName} Handles {CommandName}", type.Name, genericArguments[0].Name);
                            }

                            HandleCommandBits(commandType: genericArguments[0], commandBitTypes: CommandBitTypes.CommandHandlerRegistered);

                            var isContextAware = genericArguments[0].GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IContextAware)));

                            if (isContextAware)
                            {
                                HandleCommandBits(commandType: genericArguments[0], commandBitTypes: CommandBitTypes.ContextAware);
                            }

                            var isCorrelationIdAware = genericArguments[0].GetInterfaces().Any(i => i.IsAssignableFrom(typeof(ICorrelationAware)));

                            if (isCorrelationIdAware)
                            {
                                HandleCommandBits(commandType: genericArguments[0], commandBitTypes: CommandBitTypes.CorrelationIdAware);
                            }

                            // Only set this bit if the global option is enabled AND the command type actually has
                            // something for DataAnnotationsValidator/MiniValidation to check. This avoids the cost
                            // of constructing a validator and calling into MiniValidation for commands with nothing
                            // to validate (e.g. commands that are only validated upstream by ASP.NET model binding).
                            // Note: this bit is only about DataAnnotations validation - it must not be used to decide
                            // whether registered IValidator<TCommand> instances run; those are gated by their own
                            // CommandValidator bit and must always run when registered.
                            if (this.isValidationEnabled && MiniValidator.RequiresValidation(targetType: genericArguments[0], recurse: true))
                            {
                                HandleCommandBits(commandType: genericArguments[0], commandBitTypes: CommandBitTypes.IsValidationEnabled);
                            }

                            this.services.TryAddTransient(commandHandlerInterface, type.AsType());
                        }
                    }

                    if (isCommandValidator)
                    {
                        var commandValidatorInterfaces = type.ImplementedInterfaces.Where(x => x.IsGenericTypeOf(typeof(IValidator<ICommand>)));

                        this.logger.LogDebug("{TypeName} Contains {Count} Command Validators's", type.Name, commandValidatorInterfaces.Count());

                        foreach (var commandValidatorInterface in commandValidatorInterfaces)
                        {
                            var genericArguments = commandValidatorInterface.GetGenericArguments();

                            if (genericArguments != null && genericArguments.Count() == 1)
                            {
                                this.logger.LogDebug("{TypeName} Validates {CommandName}", type.Name, genericArguments[0].Name);
                            }

                            HandleCommandBits(commandType: genericArguments[0], commandBitTypes: CommandBitTypes.CommandValidator);

                            this.services.TryAddTransient(commandValidatorInterface, type.AsType());
                        }
                    }

                    if (isMessageHandler)
                    {
                        var queryHandlerInterfaces = type.ImplementedInterfaces.Where(x => x.IsGenericTypeOf(typeof(IMessageHandler<IMessage>)));

                        this.logger.LogDebug("{TypeName} Contains {Count} Message Handler's", type.Name, queryHandlerInterfaces.Count());

                        foreach (var queryHandlerInterface in queryHandlerInterfaces)
                        {
                            this.services.TryAddTransient(queryHandlerInterface, type.AsType());
                        }
                    }

                    if (isQueryHandler)
                    {
                        var queryHandlerInterfaces = type.ImplementedInterfaces.Where(x => x.IsGenericTypeOf(typeof(IQueryHandler<IQuery, IQueryResult>)));

                        this.logger.LogDebug("{TypeName} Contains {Count} Query Handler's", type.Name, queryHandlerInterfaces.Count());

                        foreach (var queryHandlerInterface in queryHandlerInterfaces)
                        {
                            var genericArguments = queryHandlerInterface.GetGenericArguments();

                            if (genericArguments != null && genericArguments.Count() == 2)
                            {
                                this.logger.LogDebug("{TypeName} Handles {QueryName} Returning {QueryResultName}", type.Name, genericArguments[0].Name, genericArguments[1].Name);
                            }

                            if (genericArguments != null && genericArguments.Length > 0)
                            {
                                this.queriesWithHandlers.Add(genericArguments[0]);
                            }

                            this.services.TryAddTransient(queryHandlerInterface, type.AsType());
                        }
                    }

                    if (isCommandReactor)
                    {
                        var commandReactorInterfaces = type.ImplementedInterfaces
                            .Where(x => x.IsGenericTypeOf(typeof(ICommandReactor<ICommand>)))
                            .ToArray();

                        this.logger.LogDebug("{TypeName} Contains {Count} Command Reactor's", type.Name, commandReactorInterfaces.Count());

                        if (commandReactorInterfaces.Length > 0)
                        {
                            var genericArguments = commandReactorInterfaces[0].GetGenericArguments();
                            HandleCommandBits(commandType: genericArguments[0], commandBitTypes: CommandBitTypes.CommandReactor);
                        }

                        foreach (var queryHandlerInterface in commandReactorInterfaces)
                        {
                            this.services.TryAddTransient(queryHandlerInterface, type.AsType());
                        }
                    }

                    if (isCommandResultReactor)
                    {
                        var commandResultReactorInterfaces = type.ImplementedInterfaces
                            .Where(x => x.IsGenericTypeOf(typeof(ICommandResultReactor<ICommand>)))
                            .ToArray();

                        this.logger.LogDebug("{TypeName} Contains {Count} Command Result Reactor's", type.Name, commandResultReactorInterfaces.Count());

                        if (commandResultReactorInterfaces.Length > 0)
                        {
                            var genericArguments = commandResultReactorInterfaces[0].GetGenericArguments();
                            HandleCommandBits(commandType: genericArguments[0], commandBitTypes: CommandBitTypes.CommandResultReactor);
                        }

                        foreach (var queryHandlerInterface in commandResultReactorInterfaces)
                        {
                            this.services.TryAddTransient(queryHandlerInterface, type.AsType());
                        }
                    }

                    if (isCommandInterceptor)
                    {
                        var commandInterceptorInterfaces = type.ImplementedInterfaces
                            .Where(x => x.IsGenericTypeOf(typeof(ICommandInterceptor<ICommand>)))
                            .ToArray();

                        this.logger.LogDebug("{TypeName} Contains {Count} Command Interceptor's", type.Name, commandInterceptorInterfaces.Count());

                        if (commandInterceptorInterfaces.Length > 0)
                        {
                            var genericArguments = commandInterceptorInterfaces[0].GetGenericArguments();
                            HandleCommandBits(commandType: genericArguments[0], commandBitTypes: CommandBitTypes.CommandInterceptor);
                        }

                        foreach (var coomandInterceptorInterface in commandInterceptorInterfaces)
                        {
                            this.services.TryAddTransient(coomandInterceptorInterface, type.AsType());
                        }
                    }
                }
            }
        }

        private Assembly[] LoadFromLocations()
        {
            if (this.innovationOptions.SearchLocations != null && this.innovationOptions.SearchLocations.Length > 0)
            {
                var loadedAssemblyList = new List<Assembly>();

                foreach (var searchLocation in this.innovationOptions.SearchLocations)
                {
                    if (Directory.Exists(searchLocation))
                    {
                        var files = Directory.GetFiles(searchLocation, "*.dll");

                        foreach (var file in files)
                        {
                            try
                            {
                                var loadedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);

                                loadedAssemblyList.Add(loadedAssembly);
                            }
                            catch (Exception ex)
                            {
                                this.logger.LogError(exception: ex, message: ex.GetInnerMostMessage());
                            }
                        }
                    }
                }

                return loadedAssemblyList.ToArray();
            }

            return Array.Empty<Assembly>();
        }

        private Assembly[] LoadReferencingLibraries()
        {
            var assemblies = this.GetReferencingLibraries();

            return assemblies == null ? new Assembly[] { } : assemblies.ToArray();
        }

        private IEnumerable<Assembly> GetReferencingLibraries()
        {
            try
            {
                var assembliesReferencingInnovation = new List<Assembly>();

                var runtimeLibraries = DependencyContext.Default.RuntimeLibraries;

                foreach (var runtimeLibrary in runtimeLibraries)
                {
                    if (IsCandidateLibrary(runtimeLibrary))
                    {
                        var assembly = Assembly.Load(new AssemblyName(runtimeLibrary.Name));
                        assembliesReferencingInnovation.Add(assembly);
                    }
                }

                return assembliesReferencingInnovation;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool IsCandidateLibrary(RuntimeLibrary runtimeLibrary)
        {
            return ReferenceAssemblies.Contains(runtimeLibrary.Name) || runtimeLibrary.Dependencies.Any(x => ReferenceAssemblies.Any(y => y.StartsWith(x.Name)));
        }

        private static int TurnBitOn(int value, CommandBitTypes bitToTurnOn)
        {
            return value |= 1 << (int)bitToTurnOn;
        }

        private void HandleCommandBits(Type commandType, CommandBitTypes commandBitTypes)
        {
            //if (commandBitTypes == CommandBitTypes.CommandHandlerRegistered && !this.commandLookup.ContainsKey(commandType))
            //{
            //    this.logger.LogDebug(message: $"1: \t HandleCommandBits: {commandType.Name} \t\t CommandBitType: {commandBitTypes}");

            //    this.commandLookup.TryAdd(key: commandType, value: 0);

            //    return;
            //}
            //else if (!this.commandLookup.ContainsKey(key: commandType))
            //{
            //    this.logger.LogDebug(message: $"2: \t HandleCommandBits: {commandType.Name} \t\t CommandBitType: {commandBitTypes}");

            //    this.commandLookup.TryAdd(key: commandType, value: TurnBitOn(value: 0, commandBitTypes));

            //    return;
            //}

            this.commandLookup.TryAdd(key: commandType, value: 0);
            this.logger.LogDebug(message: $"3: \t HandleCommandBits: {commandType.Name} \t\t CommandBitType: {commandBitTypes}");
            this.commandLookup[key: commandType] = TurnBitOn(value: this.commandLookup[key: commandType], commandBitTypes);

            return;
        }

        private void LogCommandBits()
        {
            foreach (var keyValuePair in this.commandLookup)
            {
                var key = keyValuePair.Key.Name;
                var bits = Convert.ToString(keyValuePair.Value, 2);

                var processCommandBitsDictionary = this.ProcessCommandBits(commandBits: keyValuePair.Value);

                this.logger.LogDebug(message: $"--------------------{key}--------------------");
                this.logger.LogDebug(message: $"Command Bits Binary: {bits}");

                this.logger.LogDebug(message: $"********************Bit By Bit********************");

                foreach (var processCommandBits in processCommandBitsDictionary)
                {
                    this.logger.LogDebug(message: $"{processCommandBits.Key} is: \t {processCommandBits}");
                }
            }

            this.logger.LogDebug(message: $"#################### End ####################");
        }

        // Cross-checks every discovered ICommand/IQuery type (see RegisterHandlers) against the handlers that
        // were actually registered. Runs once, eagerly, from Configure() - i.e. during service registration/
        // app startup, never on a per-request path - so a missing handler is caught immediately instead of
        // only being discovered the first time that specific command/query is actually dispatched.
        private void ValidateHandlersRegistered()
        {
            var commandTypesWithoutHandlers = this.discoveredCommandTypes
                .Where(commandType => (this.GetCommandBits(commandType: commandType) & (1 << (int)CommandBitTypes.CommandHandlerRegistered)) == 0)
                .ToArray();

            var queryTypesWithoutHandlers = this.discoveredQueryTypes
                .Where(queryType => !this.queriesWithHandlers.Contains(queryType))
                .ToArray();

            if (commandTypesWithoutHandlers.Length == 0 && queryTypesWithoutHandlers.Length == 0)
            {
                return;
            }

            foreach (var commandType in commandTypesWithoutHandlers)
            {
                this.logger.LogError("No ICommandHandler<{CommandType}> is registered for command {CommandType}", commandType, commandType);
            }

            foreach (var queryType in queryTypesWithoutHandlers)
            {
                this.logger.LogError("No IQueryHandler<{QueryType}, TQueryResult> is registered for query {QueryType}", queryType, queryType);
            }

            if (this.innovationOptions.FailFastOnMissingHandlers)
            {
                throw new MissingHandlersException(commandTypesWithoutHandlers: commandTypesWithoutHandlers, queryTypesWithoutHandlers: queryTypesWithoutHandlers);
            }
        }

        private Dictionary<string, int> ProcessCommandBits(int commandBits)
        {
            var commandBitTypeValues = Enum.GetValues<CommandBitTypes>();
            var commandBitDictionary = new Dictionary<string, int>();

            for (var i = 0; i < commandBitTypeValues.Length; i++)
            {
                var commandButTypeName = Enum.GetName(typeof(CommandBitTypes), commandBitTypeValues[i]);
                var value = (commandBits & (1 << i));

                commandBitDictionary.Add(key: commandButTypeName, value);
            }

            return commandBitDictionary;
        }

        #endregion Private Methods
    }
}
