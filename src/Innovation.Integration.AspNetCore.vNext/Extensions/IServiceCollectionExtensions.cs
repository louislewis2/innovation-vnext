namespace Microsoft.Extensions.DependencyInjection
{
    using System;

    using Innovation.Integration.AspNetCore.vNext;

    public static class IServiceCollectionExtensions
    {
        public static void AddInnovationAspNetIntegrationvNext(this IServiceCollection serviceCollection)
            => serviceCollection.AddInnovationAspNetIntegrationvNext(correlationIdOptions => { });

        public static void AddInnovationAspNetIntegrationvNext(this IServiceCollection serviceCollection, string correlationIdHeaderName)
            => serviceCollection.AddInnovationAspNetIntegrationvNext(correlationIdOptions => { correlationIdOptions.Header = correlationIdHeaderName; });

        public static void AddInnovationAspNetIntegrationvNext(this IServiceCollection serviceCollection, Action<CorrelationIdOptions> configureOptions)
        {
            serviceCollection.Configure(configureOptions);
        }
    }
}
