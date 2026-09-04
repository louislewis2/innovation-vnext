namespace Microsoft.Extensions.DependencyInjection
{
    using Innovation.Api.vNext.Dispatching;

    using Innovation.SampleApi.Consumer.Stores;
    using Innovation.SampleApi.Consumer.Handlers.ReactorTest;

    public static class IServiceCollectionExtensions
    {
        public static void AddConsumer(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IAuditStore, SampleAuditStore>();
            serviceCollection.AddScoped<ScopeMarker>();
        }
    }
}