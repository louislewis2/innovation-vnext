namespace Innovation.Benchmarks.Comparison.MediatorScoped
{
    using System;

    using global::Mediator;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Builds a plain ServiceProvider for this assembly's Mediator registration, configured with
    /// ServiceLifetime.Scoped to match the [assembly: MediatorOptions(...)] attribute in
    /// AssemblyInfo.cs (the runtime option passed to AddMediator must match the compile-time
    /// attribute, or Mediator throws at startup - see Mediator.g.cs's generated guard for this).
    /// </summary>
    public sealed class MediatorScopedProvider
    {
        private readonly ServiceProvider serviceProvider;

        public MediatorScopedProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddMediator(options =>
            {
                options.ServiceLifetime = ServiceLifetime.Scoped;
            });

            this.serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public T GetRequiredService<T>() where T : notnull => this.serviceProvider.GetRequiredService<T>();

        /// <summary>
        /// Creates a new DI scope from the root provider - used by the "per-request" benchmark to
        /// resolve a fresh IMediator per call, the same way ASP.NET Core creates one new scope per HTTP
        /// request (see BENCHMARKS.md, "Per-request scope cost").
        /// </summary>
        public IServiceScope CreateScope() => this.serviceProvider.CreateScope();
    }

    /// <summary>
    /// "Loaded" variant of MediatorScopedProvider - registers the same validation + logging pipeline
    /// behaviors as MediatorLoadedProvider (in the main comparison project), so
    /// Command_Mediator_Scoped_Loaded is a fair comparison against Command_Mediator_Loaded rather than
    /// only against the behavior-free Command_Mediator_Scoped.
    /// </summary>
    public sealed class MediatorScopedLoadedProvider
    {
        private readonly ServiceProvider serviceProvider;

        public MediatorScopedLoadedProvider()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(options => { options.SetMinimumLevel(LogLevel.Warning); });
            serviceCollection.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            serviceCollection.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            serviceCollection.AddMediator(options =>
            {
                options.ServiceLifetime = ServiceLifetime.Scoped;
            });

            this.serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public T GetRequiredService<T>() where T : notnull => this.serviceProvider.GetRequiredService<T>();

        /// <summary>
        /// Creates a new DI scope from the root provider - used by the "per-request" benchmark to
        /// resolve a fresh IMediator per call, the same way ASP.NET Core creates one new scope per HTTP
        /// request (see BENCHMARKS.md, "Per-request scope cost").
        /// </summary>
        public IServiceScope CreateScope() => this.serviceProvider.CreateScope();
    }
}
