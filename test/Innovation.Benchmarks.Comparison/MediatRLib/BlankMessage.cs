namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using MediatR;

    /// <summary>
    /// MediatR equivalent of InnovationLib.BlankMessage - an IO-free notification broadcast to two
    /// handlers, mirroring Innovation's Dispatcher.Message (all-handlers broadcast) shape.
    /// </summary>
    public class BlankMessage : INotification
    {
    }
}
