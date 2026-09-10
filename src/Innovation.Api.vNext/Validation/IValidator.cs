namespace Innovation.Api.vNext.Validation
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    using Commanding;

    public interface IValidator<in TCommand> where TCommand : ICommand
    {
        ValueTask<IValidationResult> Validate([DisallowNull] TCommand command, CancellationToken cancellationToken);
    }
}
