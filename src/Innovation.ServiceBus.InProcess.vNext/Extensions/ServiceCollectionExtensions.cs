namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using Extensions;

    using Innovation.Api.vNext.Reactions;
    using Innovation.Api.vNext.Dispatching;
    using Innovation.ServiceBus.InProcess.vNext;
    using Innovation.ServiceBus.InProcess.vNext.Settings;
    using Innovation.ServiceBus.InProcess.vNext.Reactions;
    using Innovation.ServiceBus.InProcess.vNext.Dispatching;

    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// This Will Add All The Required Innovation Components
        /// </summary>
        /// <example>services.AddInnovationvNext();</example>
        public static IInnovationBuilder AddInnovationvNext(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddInnovationvNext(innovationOptions => { innovationOptions.IsValidationEnabled = true; });
        }

        /// <summary>
        /// This Will Add All The Required Innovation Components and register options
        /// </summary>
        /// <example>services.AddInnovationvNext();</example>
        /// <example>services.AddInnovationvNext().WithAuditStore&lt;MyAuditStore&gt;();</example>
        public static IInnovationBuilder AddInnovationvNext(this IServiceCollection serviceCollection, Action<InnovationOptions> innovationOptions)
        {
            serviceCollection.Configure(innovationOptions);

            serviceCollection.TryAddSingleton<IReactorInvoker, ReactorInvoker>();
            serviceCollection.TryAddSingleton<IReactorWorkQueue, InMemoryReactorWorkQueue>();

            var provider = serviceCollection.BuildServiceProvider();

            var runtime = ActivatorUtilities.CreateInstance<InnovationRuntime>(provider, serviceCollection);
            serviceCollection.TryAddSingleton(runtime);

            runtime.Configure();
            serviceCollection.TryAddTransient<IDispatcher, Dispatcher>();

            return new InnovationBuilder(serviceCollection);
        }
    }
}
