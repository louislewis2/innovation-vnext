namespace Innovation.Benchmarks.Comparison
{
    using System;

    using MediatR;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Builds a plain-vanilla ServiceProvider for each of the three libraries under comparison, kept as
    /// close to each library's own "getting started" defaults as possible:
    ///   - Innovation: audit store not registered (no equivalent feature exists in MediatR/Mediator) and
    ///     validation disabled (IsValidationEnabled = false), so only core dispatch cost is measured.
    ///   - MediatR: default AddMediatR registration, no pipeline behaviors.
    ///   - Mediator (martinothamar): default AddMediator registration (Singleton lifetime, its default),
    ///     no pipeline behaviors.
    /// </summary>
    public abstract class ComparisonProviderBase
    {
        private readonly ServiceProvider serviceProvider;

        protected ComparisonProviderBase(Action<IServiceCollection> configure)
        {
            var serviceCollection = new ServiceCollection();
            configure(serviceCollection);
            this.serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public T GetRequiredService<T>() => this.serviceProvider.GetRequiredService<T>();

        /// <summary>
        /// Creates a new DI scope from the root provider - used by the "per-request" benchmarks to
        /// resolve a fresh entry point (IDispatcher/IMediator) per call, the same way ASP.NET Core
        /// creates one new scope per HTTP request (see BENCHMARKS.md, "Per-request scope cost").
        /// </summary>
        public IServiceScope CreateScope() => this.serviceProvider.CreateScope();
    }

    public sealed class InnovationProvider : ComparisonProviderBase
    {
        public InnovationProvider() : base(services =>
        {
            services.AddLogging(options => { options.SetMinimumLevel(LogLevel.Warning); });
            services.AddInnovationvNext(innovationOptions =>
            {
                innovationOptions.IsValidationEnabled = false;
            });
        })
        {
        }
    }

    public sealed class MediatRProvider : ComparisonProviderBase
    {
        public MediatRProvider() : base(services =>
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRLib.BlankCommand>());
        })
        {
        }
    }

    public sealed class MediatorProvider : ComparisonProviderBase
    {
        public MediatorProvider() : base(services =>
        {
            services.AddMediator();
        })
        {
        }
    }

    /// <summary>
    /// "Loaded" variants of the three providers above - each configured with an equivalent pair of
    /// cross-cutting concerns (validate, then log) so LoadedCommandComparisonTests measures a genuine
    /// framework-vs-framework comparison rather than Innovation's full pipeline vs a competitor with
    /// zero registered behaviors. See LoadedCommandComparisonTests and BENCHMARKS.md for the full
    /// rationale - these numbers back the tradeoffs documented there, so keep them in sync.
    /// </summary>
    public sealed class InnovationLoadedProvider : ComparisonProviderBase
    {
        public InnovationLoadedProvider() : base(services =>
        {
            services.AddLogging(options => { options.SetMinimumLevel(LogLevel.Warning); });
            // No IAuditStore is registered here - audit is an Innovation-only concept with no
            // MediatR/Mediator equivalent in this comparison, so registering one (even a no-op) would
            // make Innovation pay for a third cross-cutting concern (stopwatch + DI resolve + async
            // Log call) that neither competitor's ValidationBehavior/LoggingBehavior pair models. This
            // provider is meant to be validation + logging only, matching the other two libraries.
            services.AddInnovationvNext(innovationOptions =>
            {
                // IsValidationEnabled only controls the built-in DataAnnotations/MiniValidation pass, which
                // MediatR/Mediator have no equivalent of - it does not control whether the registered
                // IValidator<LoadedCommand> (LoadedCommandValidator) below runs; that always runs when
                // registered, gated by its own CommandValidator bit. Keeping this false is what makes the
                // "loaded" comparison an equivalent no-op validation behavior on all three libraries.
                innovationOptions.IsValidationEnabled = false;
            });
        })
        {
        }
    }

    public sealed class MediatRLoadedProvider : ComparisonProviderBase
    {
        public MediatRLoadedProvider() : base(services =>
        {
            // Same minimum level as InnovationLoadedProvider, so LoggingBehavior's guarded IsEnabled(Debug)
            // checks are gated identically on both sides of the comparison.
            services.AddLogging(options => { options.SetMinimumLevel(LogLevel.Warning); });
            services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(MediatRLib.ValidationBehavior<,>));
            services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(MediatRLib.LoggingBehavior<,>));
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRLib.BlankCommand>());
        })
        {
        }
    }

    public sealed class MediatorLoadedProvider : ComparisonProviderBase
    {
        public MediatorLoadedProvider() : base(services =>
        {
            // Same minimum level as InnovationLoadedProvider, so LoggingBehavior's guarded IsEnabled(Debug)
            // checks are gated identically on both sides of the comparison.
            services.AddLogging(options => { options.SetMinimumLevel(LogLevel.Warning); });
            services.AddSingleton(typeof(global::Mediator.IPipelineBehavior<,>), typeof(MediatorLib.ValidationBehavior<,>));
            services.AddSingleton(typeof(global::Mediator.IPipelineBehavior<,>), typeof(MediatorLib.LoggingBehavior<,>));
            services.AddMediator();
        })
        {
        }
    }
}
