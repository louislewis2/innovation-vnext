namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using Innovation.Api.vNext.Commanding;

    /// <summary>
    /// Mirrors Innovation.ApiSample.BlankCommand - an IO-free command used only to exercise Command
    /// dispatch cost in the cross-library comparison. Defined locally (rather than referencing
    /// Innovation.ApiSample) so the three libraries' contracts/handlers live side by side and stay
    /// symmetric with MediatRLib/MediatorLib.
    /// </summary>
    public class BlankCommand : ICommand
    {
        public string EventName => nameof(BlankCommand);
    }
}
