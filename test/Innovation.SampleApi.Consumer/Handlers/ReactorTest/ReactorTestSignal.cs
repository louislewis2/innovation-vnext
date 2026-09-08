namespace Innovation.SampleApi.Consumer.Handlers.ReactorTest
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    /// <summary>
    /// Test-only signaling registry so ReactorPipelineTests can await (with a timeout) confirmation that a
    /// background-queued reactor actually ran for a given correlation id, without resorting to Thread.Sleep,
    /// and can assert it ran under a different (fresh) DI scope than the one the command was originally
    /// dispatched under - a regression test for the old Task.Run-from-captured-scope bug.
    /// </summary>
    public static class ReactorTestSignal
    {
        private static readonly ConcurrentDictionary<string, Guid> dispatchScopeIds = new ConcurrentDictionary<string, Guid>();
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> commandReactorSignals = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> commandResultReactorSignals = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        public static void RecordDispatchScopeId(string correlationId, Guid scopeId)
        {
            dispatchScopeIds[correlationId] = scopeId;
        }

        // The command handler only gets to record under the correct key if the dispatcher handed it the
        // correlation id, so this doubles as the assertion that handler-level ICorrelationAware works.
        public static bool WasDispatchScopeRecordedFor(string correlationId)
        {
            return dispatchScopeIds.ContainsKey(correlationId);
        }

        public static Task<bool> WaitForCommandReactor(string correlationId)
        {
            return commandReactorSignals.GetOrAdd(correlationId, _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }

        public static Task<bool> WaitForCommandResultReactor(string correlationId)
        {
            return commandResultReactorSignals.GetOrAdd(correlationId, _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }

        // ranInFreshScope: true if the reactor's scope id differed from the scope id recorded at dispatch time.
        public static void SignalCommandReactor(string correlationId, Guid reactorScopeId)
        {
            var ranInFreshScope = !dispatchScopeIds.TryGetValue(correlationId, out var dispatchScopeId) || dispatchScopeId != reactorScopeId;

            commandReactorSignals.GetOrAdd(correlationId, _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult(ranInFreshScope);
        }

        public static void SignalCommandResultReactor(string correlationId, Guid reactorScopeId)
        {
            var ranInFreshScope = !dispatchScopeIds.TryGetValue(correlationId, out var dispatchScopeId) || dispatchScopeId != reactorScopeId;

            commandResultReactorSignals.GetOrAdd(correlationId, _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult(ranInFreshScope);
        }
    }
}
