namespace Innovation.Api.vNext.Validation
{
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    using Commanding;

    public interface IValidator<in TCommand> where TCommand : ICommand
    {
        Task<IValidationResult> Validate([DisallowNull] TCommand command);
    }
}
