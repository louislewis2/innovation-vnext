namespace Innovation.Benchmarks.ValidatorTests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    using MiniValidation;
    using BenchmarkDotNet.Attributes;
    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Dispatching;

    using Innovation.ApiSample;

    /// <summary>
    /// A benchmark class to test the performance of the Dispatcher with a specific command object (BlankCommand).
    /// </summary>
    [MemoryDiagnoser]
    public class DispatcherCommandTests : DependencyBuilderBase
    {
        #region Fields

        private IDispatcher dispatcher;
        private IServiceScopeFactory serviceScopeFactory;
        private IServiceScope serviceScope;
        private IServiceProvider serviceProvider;
        private static BlankCommand blankCommand = new BlankCommand();

        #endregion Fields

        #region Methods

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.dispatcher = this.GetRequiredService<IDispatcher>();
            this.serviceScopeFactory = this.GetRequiredService<IServiceScopeFactory>();
            this.serviceProvider = this.GetRequiredService<IServiceProvider>();
            this.serviceScope = this.serviceScopeFactory.CreateScope();
            MiniValidator.TryValidate(blankCommand, this.serviceProvider, out var tt);
        }

        [Benchmark]
        public async ValueTask<ICommandResult> DispatchBlankCommand()
        {
            return await dispatcher.Command(command: blankCommand);
        }

        #endregion Methods
    }
}
