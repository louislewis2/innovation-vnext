namespace Microsoft.Extensions.DependencyInjection
{
    using Innovation.Api.vNext.Dispatching;

    using Innovation.SampleApi.Consumer.Stores;
    using Innovation.SampleApi.Consumer.Handlers.ReactorTest;

    public static class IServiceCollectionExtensions
    {
        public static void AddConsumer(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddConsumer(includeAuditStore: true);
        }

        /// <summary>
        /// Overload allowing callers (e.g. benchmarks comparing the pipeline with/without an audit store)
        /// to opt out of the IAuditStore registration while still getting the rest of the consumer's
        /// sample handlers/services.
        /// </summary>
        public static void AddConsumer(this IServiceCollection serviceCollection, bool includeAuditStore)
        {
            if (includeAuditStore)
            {
                serviceCollection.AddSingleton<IAuditStore, SampleAuditStore>();
            }

            serviceCollection.AddScoped<ScopeMarker>();
        }
    }
}