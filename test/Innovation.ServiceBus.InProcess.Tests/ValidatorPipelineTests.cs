namespace Innovation.ServiceBus.InProcess.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.CommandHelpers;

    using ApiSample.Validation;
    using ApiSample.Shared.Criteria;
    using ApiSample.Vendors.Criteria;
    using ApiSample.Vendors.Commands;
    using ApiSample.Suppliers.Criteria;
    using ApiSample.Suppliers.Commands;

    [TestClass]
    public class ValidatorPipelineTests : TestBase
    {
        #region Methods

        [TestMethod]
        public async Task Can_Receive_ValidationResult()
        {
            // Arrange
            var supplierCriteria = new SupplierCriteria(name: "CoolName");
            var insertSupplierCommand = new InsertSupplierCommand(supplierCriteria: supplierCriteria);

            // Act
            var dispatcher = this.GetDispatcher();
            var commandResult = (await dispatcher.Command(command: insertSupplierCommand, cancellationToken: CancellationToken.None)).As<SampleValidationResult>();

            // Assert
            Assert.IsFalse(condition: commandResult.Success);
            Assert.AreEqual(expected: "Another Test Error Message", actual: commandResult.Errors[0].ErrorMessage);
        }

        [TestMethod]
        public async Task Can_Invoke_Command_Validator()
        {
            // Arrange
            // Arrange
            var vendorCriteria = new VendorCriteria(
                name: "Innovation",
                userName: "SomeUserNameThatRocks",
                addressCriteria: new AddressCriteria(line1: "111 Street", code: null));
            var insertVendorCommand = new InsertVendorCommand(vendorCriteria: vendorCriteria);

            // Act
            var dispatcher = this.GetDispatcher();
            var commandResult = (await dispatcher.Command(command: insertVendorCommand, cancellationToken: CancellationToken.None)).As<SampleValidationResult>();

            // Assert
            Assert.IsFalse(condition: commandResult.Success);
            Assert.AreEqual(expected: "Street Cannot Be 111 Street", actual: commandResult.Errors[0].ErrorMessage);
        }

        [TestMethod]
        public async Task Can_Invoke_Command_Validator_When_DataAnnotations_Also_Fails()
        {
            // Arrange
            // Name is null (fails the [Required] DataAnnotations attribute) AND Line1 is "111 Street"
            // (fails the custom InsertVendorAddressValidator). The custom validator must still run and its
            // result must win, even though DataAnnotations validation failed first - this is the bug that was
            // fixed: previously the custom validator was skipped entirely whenever DataAnnotations failed.
            var vendorCriteria = new VendorCriteria(
                name: null,
                userName: "SomeUserNameThatRocks",
                addressCriteria: new AddressCriteria(line1: "111 Street", code: null));
            var insertVendorCommand = new InsertVendorCommand(vendorCriteria: vendorCriteria);

            // Act
            var dispatcher = this.GetDispatcher();
            var commandResult = (await dispatcher.Command(command: insertVendorCommand, cancellationToken: CancellationToken.None)).As<SampleValidationResult>();

            // Assert
            Assert.IsFalse(condition: commandResult.Success);
            Assert.AreEqual(expected: "Street Cannot Be 111 Street", actual: commandResult.Errors[0].ErrorMessage);
        }

        [TestMethod]
        public async Task Can_Return_DataAnnotations_Errors_When_Custom_Validator_Passes()
        {
            // Arrange
            // Name is null (fails DataAnnotations) but Line1 is valid (custom validator passes). The
            // DataAnnotations error must still surface as the final result.
            var vendorCriteria = new VendorCriteria(
                name: null,
                userName: "SomeUserNameThatRocks",
                addressCriteria: new AddressCriteria(line1: "1 Main Street", code: null));
            var insertVendorCommand = new InsertVendorCommand(vendorCriteria: vendorCriteria);

            // Act
            var dispatcher = this.GetDispatcher();
            var commandResult = (await dispatcher.Command(command: insertVendorCommand, cancellationToken: CancellationToken.None)).As<CommandResult>();

            // Assert
            Assert.IsFalse(condition: commandResult.Success);
            Assert.AreEqual(expected: "The Name field is required.", actual: commandResult[0].Reasons[0]);
        }

        #endregion Methods
    }
}
