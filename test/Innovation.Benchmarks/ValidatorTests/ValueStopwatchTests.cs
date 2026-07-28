namespace Innovation.Benchmarks.ValidatorTests
{
    using BenchmarkDotNet.Attributes;

    using Innovation.Api.vNext.Dispatching;

    [MemoryDiagnoser]
    public class ValueStopwatchTests : DependencyBuilderBase
    {
        #region Methods

        [Benchmark]
        public long StopWatchUsage()
        {
            var stopWatch = ValueStopwatch.StartNew();

            return (long)stopWatch.GetElapsedTime().TotalMilliseconds;

        }

        #endregion Methods
    }
}
