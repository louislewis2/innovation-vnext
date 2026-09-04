namespace Innovation.ServiceBus.InProcess.vNext.Settings
{
    public class InnovationOptions
    {
        #region Properties

        // If false, the dispatcher will not perform validation on commands
        public bool IsValidationEnabled { get; set; }

        // If true, all registered validation (DataAnnotations and IValidator<TCommand>) will run and have their
        // errors merged into a single result. If false (default), validation fails fast on the first failure
        // encountered, matching prior behavior.
        public bool AggregateValidationErrors { get; set; }

        // Search locations can contain dll's which will be dynamically loaded and processed
        public string[] SearchLocations { get; set; }

        // if false the runtime will not search for assemblies to dynamically load from search locations
        public bool DisableDynamicLoading { get; set; }

        // The maximum number of ReactorWorkItem instances the default in-memory reactor work queue will
        // hold at once. Only relevant to the default IReactorWorkQueue (InMemoryReactorWorkQueue) - custom
        // implementations are free to ignore this setting entirely.
        public int ReactorQueueCapacity { get; set; } = 10_000;

        // What the default in-memory reactor work queue does when ReactorQueueCapacity is reached.
        // Defaults to DropOldest so that a burst of reactor work can never cause Dispatcher.Command to
        // block waiting for queue space - dispatch must never be slowed down by reactor work.
        public System.Threading.Channels.BoundedChannelFullMode ReactorQueueFullMode { get; set; } = System.Threading.Channels.BoundedChannelFullMode.DropOldest;

        // How long the default in-memory reactor work queue will wait, when asked to drain (e.g. during a
        // graceful ASP.NET Core shutdown), for already-queued/in-flight reactor work to finish before
        // giving up and letting the process exit anyway.
        public System.TimeSpan ReactorQueueShutdownDrainTimeout { get; set; } = System.TimeSpan.FromSeconds(5);

        #endregion Properties
    }
}
