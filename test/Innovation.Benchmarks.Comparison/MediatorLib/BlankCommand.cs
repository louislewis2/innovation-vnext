namespace Innovation.Benchmarks.Comparison.MediatorLib
{
    using global::Mediator;

    /// <summary>
    /// Mediator (martinothamar) equivalent of InnovationLib.BlankCommand - an IO-free command with a
    /// result, mirroring Innovation's ICommand/ICommandResult shape (Success flag).
    /// </summary>
    public class BlankCommand : ICommand<BlankCommandResult>
    {
    }

    public class BlankCommandResult
    {
        public bool Success => true;
    }
}
