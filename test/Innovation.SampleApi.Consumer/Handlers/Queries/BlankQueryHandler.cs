namespace Innovation.SampleApi.Consumer.Handlers.Queries
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Querying;

    using Innovation.ApiSample;

    /// <summary>
    /// Handles BlankQuery - kept deliberately free of any IO so Query dispatch benchmarks measure only
    /// the framework's overhead, not any downstream work.
    /// </summary>
    public class BlankQueryHandler : IQueryHandler<BlankQuery, BlankQueryResult>
    {
        #region Fields

        private static readonly BlankQueryResult queryResult = new BlankQueryResult();

        #endregion Fields

        #region Methods

        public ValueTask<BlankQueryResult> Handle(BlankQuery query)
        {
            return ValueTask.FromResult(queryResult);
        }

        #endregion Methods
    }
}
