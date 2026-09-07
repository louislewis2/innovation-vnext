namespace Innovation.Benchmarks.Comparison.MediatorScoped
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Running;

    public class Program
    {
        public static async Task Main(string[] args)
        {
#if DEBUG

            var comparisonTests = new MediatorScopedComparisonTests();
            comparisonTests.GlobalSetup();
            await comparisonTests.Command_Mediator_Scoped();

            var loadedComparisonTests = new MediatorScopedLoadedComparisonTests();
            loadedComparisonTests.GlobalSetup();
            await loadedComparisonTests.Command_Mediator_Scoped_Loaded();

            var summary = BenchmarkRunner.Run<MediatorScopedComparisonTests>(config:
                DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator));

            var loadedSummary = BenchmarkRunner.Run<MediatorScopedLoadedComparisonTests>(config:
                DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator));
#else
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithSummaryStyle(summaryStyle: BenchmarkDotNet.Reports.SummaryStyle.Default.WithRatioStyle(ratioStyle: BenchmarkDotNet.Columns.RatioStyle.Trend)));

#endif
        }
    }
}
