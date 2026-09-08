namespace Innovation.ServiceBus.InProcess.Tests
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Innovation.Api.vNext.Querying;
    using Innovation.Api.vNext.Messaging;
    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Reactions;
    using Innovation.Api.vNext.Dispatching;

    using Innovation.ApiSample;
    using Innovation.SampleApi.Consumer.Handlers.ReactorTest;

    [TestClass]
    public class ReactorPipelineTests : TestBase
    {
        private static readonly TimeSpan waitTimeout = TimeSpan.FromSeconds(5);

        [TestMethod]
        public async Task Dispatch_Returns_Successfully_Even_Though_Reactors_Are_Registered()
        {
            // Arrange
            var command = new ReactorTestCommand();
            var dispatcher = this.GetDispatcher();

            // Act
            var result = await dispatcher.Command(command: command, suppressExceptions: false);

            // Assert - dispatch must complete and return successfully without the caller having to wait for
            // any reactor to run. Reactor timing itself is covered by the benchmark suite, not this test.
            Assert.IsTrue(condition: result.Success);
        }

        [TestMethod]
        public async Task Command_Handler_Receives_The_Correlation_Id()
        {
            // Arrange
            var command = new ReactorTestCommand();
            var dispatcher = this.GetDispatcher();
            var correlationId = Guid.NewGuid().ToString();
            dispatcher.SetCorrelationId(correlationId: correlationId);

            // Act
            var result = await dispatcher.Command(command: command, suppressExceptions: false);

            // Assert - the handler implements ICorrelationAware and records under whatever correlation id it
            // was given, so finding the key proves the dispatcher stamped the handler rather than the command.
            Assert.IsTrue(condition: result.Success);
            Assert.IsTrue(condition: ReactorTestSignal.WasDispatchScopeRecordedFor(correlationId: correlationId), message: "The command handler did not receive the dispatcher's correlation id.");
        }

        [TestMethod]
        public async Task Command_Reactor_Eventually_Runs_In_A_Fresh_Scope()
        {
            // Arrange
            var command = new ReactorTestCommand();
            var dispatcher = this.GetDispatcher();
            var correlationId = Guid.NewGuid().ToString();
            dispatcher.SetCorrelationId(correlationId: correlationId);

            var waitForReactorTask = ReactorTestSignal.WaitForCommandReactor(correlationId: correlationId);

            // Act
            var result = await dispatcher.Command(command: command, suppressExceptions: false);

            var ranInFreshScope = await waitForReactorTask.WaitAsync(timeout: waitTimeout);

            // Assert
            Assert.IsTrue(condition: result.Success);
            Assert.IsTrue(condition: ranInFreshScope, message: "The command reactor ran under the same scope the command was dispatched under - it should always run in a fresh scope.");
        }

        [TestMethod]
        public async Task Command_Result_Reactor_Eventually_Runs_In_A_Fresh_Scope()
        {
            // Arrange
            var command = new ReactorTestCommand();
            var dispatcher = this.GetDispatcher();
            var correlationId = Guid.NewGuid().ToString();
            dispatcher.SetCorrelationId(correlationId: correlationId);

            var waitForReactorTask = ReactorTestSignal.WaitForCommandResultReactor(correlationId: correlationId);

            // Act
            var result = await dispatcher.Command(command: command, suppressExceptions: false);

            var ranInFreshScope = await waitForReactorTask.WaitAsync(timeout: waitTimeout);

            // Assert
            Assert.IsTrue(condition: result.Success);
            Assert.IsTrue(condition: ranInFreshScope, message: "The command result reactor ran under the same scope the command was dispatched under - it should always run in a fresh scope.");
        }

        [TestMethod]
        public void WithReactorWorkQueue_Overrides_The_Default_InMemory_Queue()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();

            // Act
            services.AddInnovationvNext().WithReactorWorkQueue<TestReactorWorkQueue>();
            var provider = services.BuildServiceProvider();

            // Assert
            var reactorWorkQueue = provider.GetRequiredService<IReactorWorkQueue>();
            Assert.IsInstanceOfType<TestReactorWorkQueue>(value: reactorWorkQueue);
        }

        [TestMethod]
        public async Task WithAuditStore_Is_Used_Even_Though_Chained_After_AddInnovationvNext()
        {
            // Arrange - deliberately chains .WithAuditStore<T>() AFTER AddInnovationvNext(), which is exactly
            // the ordering that broke HasAuditStoreRegistered detection (a one-time eager check, computed
            // against a temporary provider snapshot taken before this call could run) prior to the fix that
            // made the check lazy, evaluated against the real, fully-built provider on first dispatch.
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddLogging();
            services.AddConsumer();

            services.AddInnovationvNext().WithAuditStore<TestAuditStore>();

            var provider = services.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<IDispatcher>();

            TestAuditStore.LoggedCommands.Clear();

            // Act
            var result = await dispatcher.Command(command: new ReactorTestCommand(), suppressExceptions: false);

            // Assert
            Assert.IsTrue(condition: result.Success);
            Assert.AreEqual(expected: 1, actual: TestAuditStore.LoggedCommands.Count, message: "TestAuditStore.Log should have been called - HasAuditStoreRegistered must reflect the audit store registered via WithAuditStore<T>(), even though it was chained after AddInnovationvNext().");
        }

        #region Test Doubles

        private class TestReactorWorkQueue : IReactorWorkQueue
        {
            public ValueTask EnqueueAsync(ReactorWorkItem item, System.Threading.CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        }

        private class TestAuditStore : IAuditStore
        {
            public static ConcurrentBag<ICommand> LoggedCommands { get; } = new ConcurrentBag<ICommand>();

            public Task Log(AuditContext auditContext, ICommand command, ICommandResult commandResult)
            {
                LoggedCommands.Add(command);

                return Task.CompletedTask;
            }

            public Task Log(AuditContext auditContext, IQuery query) => Task.CompletedTask;

            public Task Log(AuditContext auditContext, IMessage message) => Task.CompletedTask;
        }

        #endregion Test Doubles
    }
}
