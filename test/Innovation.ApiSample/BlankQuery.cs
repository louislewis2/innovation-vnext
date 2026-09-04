namespace Innovation.ApiSample
{
    using Innovation.Api.vNext.Querying;

    /// <summary>
    /// A dedicated, IO-free query used only to exercise the Query dispatch pipeline in benchmarks,
    /// analogous to BlankCommand for the Command pipeline.
    /// </summary>
    public class BlankQuery : IQuery
    {
        public string EventName => nameof(BlankQuery);
    }
}
