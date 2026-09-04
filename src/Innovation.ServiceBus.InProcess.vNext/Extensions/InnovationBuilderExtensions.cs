namespace Microsoft.Extensions.DependencyInjection
{
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using Innovation.Api.vNext.Reactions;
    using Innovation.Api.vNext.Dispatching;

    public static class InnovationBuilderExtensions
    {
        /// <summary>
        /// Registers <typeparamref name="TAuditStore"/> as the <see cref="IAuditStore"/> implementation,
        /// replacing whichever implementation (default: none) AddInnovationvNext() registered.
        /// </summary>
        /// <example>services.AddInnovationvNext().WithAuditStore&lt;MyAuditStore&gt;();</example>
        public static IInnovationBuilder WithAuditStore<TAuditStore>(this IInnovationBuilder builder) where TAuditStore : class, IAuditStore
        {
            // Replace (not TryAdd) because AddInnovationvNext() has already registered its own default via
            // TryAdd by the time this runs - TryAdd here would silently no-op and this call would have no effect.
            builder.Services.Replace(ServiceDescriptor.Transient<IAuditStore, TAuditStore>());

            return builder;
        }

        /// <summary>
        /// Registers <typeparamref name="TReactorWorkQueue"/> as the <see cref="IReactorWorkQueue"/>
        /// implementation, replacing the default in-memory, best-effort queue. Use this to plug in a
        /// durable queue (e.g. backed by an outbox table, SQS, or Kafka) when reactor work must survive a
        /// process restart.
        /// </summary>
        /// <example>services.AddInnovationvNext().WithReactorWorkQueue&lt;MyDurableReactorWorkQueue&gt;();</example>
        public static IInnovationBuilder WithReactorWorkQueue<TReactorWorkQueue>(this IInnovationBuilder builder) where TReactorWorkQueue : class, IReactorWorkQueue
        {
            builder.Services.Replace(ServiceDescriptor.Singleton<IReactorWorkQueue, TReactorWorkQueue>());

            return builder;
        }
    }
}
