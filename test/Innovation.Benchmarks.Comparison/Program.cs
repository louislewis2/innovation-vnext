namespace Innovation.Benchmarks.Comparison
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;

    public class Program
    {
        public static async Task Main(string[] args)
        {
#if DEBUG

            var commandComparisonTests = new CommandComparisonTests();
            commandComparisonTests.GlobalSetup();
            await commandComparisonTests.Command_Innovation();
            await commandComparisonTests.Command_MediatR();
            await commandComparisonTests.Command_Mediator();

            var queryComparisonTests = new QueryComparisonTests();
            queryComparisonTests.GlobalSetup();
            await queryComparisonTests.Query_Innovation();
            await queryComparisonTests.Query_MediatR();
            await queryComparisonTests.Query_Mediator();

            var messageComparisonTests = new MessageComparisonTests();
            messageComparisonTests.GlobalSetup();
            await messageComparisonTests.Message_Innovation();
            await messageComparisonTests.Message_MediatR();
            await messageComparisonTests.Message_Mediator();

            var loadedCommandComparisonTests = new LoadedCommandComparisonTests();
            loadedCommandComparisonTests.GlobalSetup();
            await loadedCommandComparisonTests.Command_Innovation_Loaded();
            await loadedCommandComparisonTests.Command_MediatR_Loaded();
            await loadedCommandComparisonTests.Command_Mediator_Loaded();

            var summary = BenchmarkRunner.Run<CommandComparisonTests>(config:
                DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator));
#else
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithSummaryStyle(summaryStyle: SummaryStyle.Default.WithRatioStyle(ratioStyle: BenchmarkDotNet.Columns.RatioStyle.Trend)));

#endif
        }
    }
}
