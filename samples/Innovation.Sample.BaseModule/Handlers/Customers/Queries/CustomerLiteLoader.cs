namespace Innovation.Sample.BaseModule.Handlers.Customers.Queries
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.EntityFrameworkCore;

    using Innovation.Api.vNext.Querying;

    using Innovation.Sample.Api.Paging;
    using Innovation.Sample.Data.Contexts;
    using Innovation.Sample.Api.Customers.Queries;
    using Innovation.Sample.Data.Anemics.Customers;
    using Innovation.Sample.Api.Customers.ViewModels;

    public class CustomerLiteLoader :
        IQueryHandler<GetCustomerQuery, CustomerLite>,
        IQueryHandler<QueryPage, GenericResultsList<CustomerLite>>
    {
        #region Fields

        private readonly ILogger logger;
        private readonly PrimaryContext primaryContext;

        #endregion Fields

        #region Constructor

        public CustomerLiteLoader(ILogger<CustomerLiteLoader> logger, PrimaryContext primaryContext)
        {
            this.logger = logger;
            this.primaryContext = primaryContext;
        }

        #endregion Constructor

        #region Methods

        public async ValueTask<CustomerLite> Handle(GetCustomerQuery query, CancellationToken cancellationToken)
        {
            return await Load(query: query, cancellationToken: cancellationToken);
        }

        public async ValueTask<GenericResultsList<CustomerLite>> Handle(QueryPage query, CancellationToken cancellationToken)
        {
            return await Load(query: query, cancellationToken: cancellationToken);
        }

        #endregion Methods

        #region Private Methods

        private async Task<CustomerLite> Load(GetCustomerQuery query, CancellationToken cancellationToken)
        {
            var customer = await primaryContext.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == query.CustomerId, cancellationToken);

            return customer.ToCustomerLite();
        }

        private async Task<GenericResultsList<CustomerLite>> Load(QueryPage query, CancellationToken cancellationToken)
        {
            var serverCount = 0;
            var customersQueryable = primaryContext.Customers
                .AsNoTracking()
                .AsQueryable();

            // Opt in because it results in a extra database hit
            if (query.IncludeServerCount)
            {
                serverCount = await customersQueryable.CountAsync(cancellationToken);
            }

            var itemsPaged = await customersQueryable.Page(queryPage: query).ToArrayAsync(cancellationToken);

            return new GenericResultsList<CustomerLite>(itemsPaged.ToCustomerLite(), new QueryPagingInfo(query.Page, query.PageSize, serverCount));
        }

        #endregion Private Methods
    }
}
