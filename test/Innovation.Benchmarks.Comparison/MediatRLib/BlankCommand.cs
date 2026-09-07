namespace Innovation.Benchmarks.Comparison.MediatRLib
{
    using MediatR;

    /// <summary>
    /// MediatR equivalent of InnovationLib.BlankCommand - an IO-free request with a result, mirroring
    /// Innovation's ICommand/ICommandResult shape (Success flag) as closely as MediatR's model allows.
    /// </summary>
    public class BlankCommand : IRequest<BlankCommandResult>
    {
    }

    public class BlankCommandResult
    {
        public bool Success => true;
    }
}
