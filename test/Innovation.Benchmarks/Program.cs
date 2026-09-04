namespace Innovation.Benchmarks
{
    using System.Threading.Tasks;

    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;

    using Innovation.Benchmarks.PipelineTests;

    public class Program
    {
        public static async Task Main(string[] args)
        {
#if DEBUG

            var dispatcherCommandTests = new DispatcherCommandTests();
            dispatcherCommandTests.GlobalSetup();
            await dispatcherCommandTests.DispatchBlankCommand();

            var dispatcherQueryTests = new DispatcherQueryTests();
            dispatcherQueryTests.GlobalSetup();
            await dispatcherQueryTests.DispatchBlankQuery();

            var dispatcherMessageTests = new DispatcherMessageTests();
            dispatcherMessageTests.GlobalSetup();
            await dispatcherMessageTests.DispatchMessage();
            await dispatcherMessageTests.DispatchMessageFor();

            var pipelineComparisonTests = new DispatcherPipelineComparisonTests();
            pipelineComparisonTests.GlobalSetup();
            await pipelineComparisonTests.Blank();
            await pipelineComparisonTests.ReactorAndResultReactor();
            await pipelineComparisonTests.Interceptor();
            await pipelineComparisonTests.DataAnnotationsAndCustomValidator();

            var auditStoreComparisonTests = new AuditStoreComparisonTests();
            auditStoreComparisonTests.GlobalSetup();
            await auditStoreComparisonTests.AuditStoreRegistered();
            await auditStoreComparisonTests.AuditStoreNotRegistered();

            var validationAggregationComparisonTests = new ValidationAggregationComparisonTests();
            validationAggregationComparisonTests.GlobalSetup();
            await validationAggregationComparisonTests.FailFast();
            await validationAggregationComparisonTests.Aggregate();

            //var dataAnnotationsValidatorTests = new DataAnnotationsValidatorTests();
            //dataAnnotationsValidatorTests.GlobalSetup();
            //dataAnnotationsValidatorTests.TestNew();

            var summary = BenchmarkRunner.Run<DataAnnotationsValidatorTests>(config:
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
