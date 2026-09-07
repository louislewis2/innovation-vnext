namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using global::Mediator;

    /// <summary>
    /// Mediator (martinothamar) equivalent of InnovationLib.BlankMessage - an IO-free notification
    /// broadcast to two handlers, mirroring Innovation's Dispatcher.Message (all-handlers broadcast).
    /// </summary>
    public class BlankMessage : INotification
    {
    }
}
