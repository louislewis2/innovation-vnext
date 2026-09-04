namespace Innovation.Api.vNext.Validation
{
    using System.Collections.Generic;

    using Commanding;

    public interface IValidationResult : ICommandResult
    {
        /// <summary>
        /// The structured validation errors for this result, keyed by member name. This is opt-in: implementations
        /// that don't override this member (the default below) simply won't participate in error aggregation when
        /// InnovationOptions.AggregateValidationErrors is enabled, and will continue to work exactly as before,
        /// since this default keeps the interface non-breaking for existing implementations.
        /// </summary>
        IDictionary<string, string[]> Errors => null;
    }
}
