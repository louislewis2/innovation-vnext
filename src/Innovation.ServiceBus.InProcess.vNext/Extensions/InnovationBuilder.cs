namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class InnovationBuilder : IInnovationBuilder
    {
        public InnovationBuilder(IServiceCollection services)
        {
            this.Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
