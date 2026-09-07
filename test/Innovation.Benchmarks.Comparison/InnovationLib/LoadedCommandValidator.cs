namespace Innovation.Benchmarks.Comparison.InnovationLib
{
    using System.Threading.Tasks;

    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Validation;

    /// <summary>
    /// No-op validator for LoadedCommand - always valid, kept IO-free. Its only purpose is to turn on
    /// Innovation's validation bit/branch for this command type, mirroring a MediatR/Mediator
    /// "ValidationBehavior" pipeline behavior that always passes.
    /// </summary>
    public class LoadedCommandValidator : IValidator<LoadedCommand>
    {
        private static readonly IValidationResult validResult = new ValidResult();

        public ValueTask<IValidationResult> Validate(LoadedCommand command)
        {
            return ValueTask.FromResult(validResult);
        }

        private sealed class ValidResult : IValidationResult
        {
            public bool Success => true;
        }
    }
}
