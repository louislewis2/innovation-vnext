namespace Innovation.SampleApi.Consumer.Handlers.ReactorTest
{
    using System;

    /// <summary>
    /// Test-only marker with an id fixed for the lifetime of whichever DI scope resolves it - used to prove
    /// a reactor ran under a different (fresh) scope than the command's original dispatch scope.
    /// </summary>
    public class ScopeMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}
