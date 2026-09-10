namespace Innovation.ServiceBus.InProcess.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Validation;
    using Innovation.ServiceBus.InProcess.vNext.Settings;

    using ApiSample.Shared.Criteria;
    using ApiSample.Vendors.Criteria;
    using ApiSample.Vendors.Commands;

    [TestClass]
    public class AggregateValidationTests : TestBase
    {
        #region Constructor

        public AggregateValidationTests() : base(configureOptions: (InnovationOptions options) => options.AggregateValidationErrors = true)
        {
        }

        #endregion Constructor

        #region Methods

        [TestMethod]
        public async Task Can_Aggregate_DataAnnotations_And_Custom_Validator_Errors()
        {
            // Arrange
            // Name is null (fails DataAnnotations [Required]) AND Line1 is "111 Street" (fails the custom
            // InsertVendorAddressValidator, which does not implement IValidationResult.Errors and so falls
            // back to a generic entry keyed by the validator's type name).
            var vendorCriteria = new VendorCriteria(
                name: null,
                userName: "SomeUserNameThatRocks",
                addressCriteria: new AddressCriteria(line1: "111 Street", code: null));
            var insertVendorCommand = new InsertVendorCommand(vendorCriteria: vendorCriteria);

            // Act
            var dispatcher = this.GetDispatcher();
            var commandResult = (await dispatcher.Command(command: insertVendorCommand, cancellationToken: CancellationToken.None)).As<AggregateValidationResult>();

            // Assert
            Assert.IsFalse(condition: commandResult.Success);
            Assert.AreEqual(expected: 2, actual: commandResult.Errors.Count);
            Assert.AreEqual(expected: "The Name field is required.", actual: commandResult.Errors["Criteria.Name"][0]);
            Assert.AreEqual(expected: "Validation failed", actual: commandResult.Errors["InsertVendorAddressValidator"][0]);
        }

        [TestMethod]
        public async Task Can_Aggregate_When_Only_DataAnnotations_Fails()
        {
            // Arrange
            // Name is null (fails DataAnnotations) but Line1 is valid, so the custom validator passes.
            var vendorCriteria = new VendorCriteria(
                name: null,
                userName: "SomeUserNameThatRocks",
                addressCriteria: new AddressCriteria(line1: "1 Main Street", code: null));
            var insertVendorCommand = new InsertVendorCommand(vendorCriteria: vendorCriteria);

            // Act
            var dispatcher = this.GetDispatcher();
            var commandResult = (await dispatcher.Command(command: insertVendorCommand, cancellationToken: CancellationToken.None)).As<AggregateValidationResult>();

            // Assert
            Assert.IsFalse(condition: commandResult.Success);
            Assert.AreEqual(expected: 1, actual: commandResult.Errors.Count);
            Assert.AreEqual(expected: "The Name field is required.", actual: commandResult.Errors["Criteria.Name"][0]);
        }

        [TestMethod]
        public async Task Can_Succeed_When_Aggregate_Mode_And_All_Validations_Pass()
        {
            // Arrange
            var vendorCriteria = new VendorCriteria(
                name: "Innovation",
                userName: "SomeUserNameThatRocks",
                addressCriteria: new AddressCriteria(line1: "1 Main Street", code: "0001"));
            var insertVendorCommand = new InsertVendorCommand(vendorCriteria: vendorCriteria);

            // Act
            var dispatcher = this.GetDispatcher();
            var commandResult = await dispatcher.Command(command: insertVendorCommand, cancellationToken: CancellationToken.None);

            // Assert
            Assert.IsTrue(condition: commandResult.Success);
            Assert.IsNotInstanceOfType<AggregateValidationResult>(value: commandResult);
        }

        #endregion Methods
    }
}
