namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using Extensions;

    using Innovation.Api.vNext.Dispatching;
    using Innovation.ServiceBus.InProcess.vNext;
    using Innovation.ServiceBus.InProcess.vNext.Settings;
    using Innovation.ServiceBus.InProcess.vNext.Dispatching;

    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// This Will Add All The Required Innovation Components
        /// It Also Allows Registering An Implementation Of The Audit Store Interface
        /// </summary>
        /// <example>services.AddInnovationvNext<MyAuditStore>();</example>
        public static void AddInnovationvNext<IAuditHandler>(this IServiceCollection serviceCollection) where IAuditHandler : class, IAuditStore
        {
            serviceCollection.TryAddTransient<IAuditStore, IAuditHandler>();
            serviceCollection.AddInnovationvNext();
        }

        /// <summary>
        /// This Will Add All The Required Innovation Components
        /// </summary>
        /// <example>services.AddInnovationvNext();</example>
        public static void AddInnovationvNext(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddInnovationvNext(innovationOptions => { innovationOptions.IsValidationEnabled = true; });
        }

        /// <summary>
        /// This Will Add All The Required Innovation Components and register options
        /// </summary>
        /// <example>services.AddInnovationvNext();</example>
        public static void AddInnovationvNext(this IServiceCollection serviceCollection, Action<InnovationOptions> innovationOptions)
        {
            serviceCollection.Configure(innovationOptions);
            var provider = serviceCollection.BuildServiceProvider();

            var runtime = ActivatorUtilities.CreateInstance<InnovationRuntime>(provider, serviceCollection);
            serviceCollection.TryAddSingleton(runtime);

            runtime.Configure();
            serviceCollection.TryAddTransient<IDispatcher, Dispatcher>();
        }
    }
}