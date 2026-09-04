namespace Innovation.Benchmarks.PipelineTests
{
    using System;
    using System.Threading.Tasks;

    using MiniValidation;
    using BenchmarkDotNet.Attributes;
    using Innovation.ServiceBus.InProcess.vNext.Validators;

    using Innovation.ApiSample.Customers.Commands;
    using Innovation.ApiSample.Customers.Criteria;

    /// <summary>
    /// A benchmark class to test the performance of the DataAnnotationsValidator with a specific command object (InsertCustomer).
    /// </summary>
    [MemoryDiagnoser]
    public class DataAnnotationsValidatorTests : DependencyBuilderBase
    {
        #region Fields

        private IServiceProvider serviceProvider;
        private DataAnnotationsValidator dataAnnotationsValidatorNew;
        private static CustomerCriteria customerCriteria = new CustomerCriteria(
            name: "Louis",
            userName: "louislewis2");
        private static InsertCustomerCommand insertCustomer = new InsertCustomerCommand(customerCriteria: customerCriteria);

        #endregion Fields

        #region Methods

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.serviceProvider = this.GetRequiredService<IServiceProvider>();
            this.dataAnnotationsValidatorNew = new DataAnnotationsValidator(serviceProvider: serviceProvider);
            MiniValidator.TryValidate(target: insertCustomer, this.serviceProvider, out var errors);
        }

        [Benchmark]
        public async Task<bool> InsertCustomerCommand()
        {
            var validationResult =  await dataAnnotationsValidatorNew.TryValidateObjectRecursive(target: insertCustomer);

            return validationResult.isValid;
        }

        #endregion Methods
    }
}
