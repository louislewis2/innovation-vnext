namespace Innovation.Api.vNext.Validation
{
    using System.Collections.Generic;

    /// <summary>
    /// A validation result that merges the errors from multiple validation sources (DataAnnotations and/or
    /// registered <see cref="IValidator{TCommand}"/> instances) into a single set of errors. Used by the dispatcher
    /// when error aggregation is enabled (see InnovationOptions.AggregateValidationErrors), instead of the default
    /// fail-fast behavior of stopping at the first validation failure encountered.
    /// </summary>
    public class AggregateValidationResult : IValidationResult
    {
        #region Fields

        // Intentionally left null until the first failure is recorded, to avoid allocating for the common
        // case where a command passes validation.
        private Dictionary<string, List<string>> errors;

        #endregion Fields

        #region Properties

        public bool Success => this.errors == null || this.errors.Count == 0;

        public IDictionary<string, string[]> Errors
        {
            get
            {
                if (this.errors == null)
                {
                    return null;
                }

                var result = new Dictionary<string, string[]>(this.errors.Count);

                foreach (var errorEntry in this.errors)
                {
                    result.Add(key: errorEntry.Key, value: errorEntry.Value.ToArray());
                }

                return result;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Merges an already-shaped error dictionary (e.g. from DataAnnotations validation, or a validator that
        /// implements <see cref="IValidationResult.Errors"/>) into this aggregate result.
        /// </summary>
        public void Merge(IDictionary<string, string[]> errorsToMerge)
        {
            if (errorsToMerge == null || errorsToMerge.Count == 0)
            {
                return;
            }

            foreach (var errorEntry in errorsToMerge)
            {
                this.AddErrors(key: errorEntry.Key, messages: errorEntry.Value);
            }
        }

        /// <summary>
        /// Records a generic failure for a validator that did not opt into exposing structured
        /// <see cref="IValidationResult.Errors"/>. The failure is keyed by the validator's type name so it can
        /// still be traced back to its source.
        /// </summary>
        public void MergeFallback(string validatorTypeName)
        {
            this.AddErrors(
                key: string.IsNullOrWhiteSpace(validatorTypeName) ? "Validator" : validatorTypeName,
                messages: new[] { "Validation failed" });
        }

        #endregion Methods

        #region Private Methods

        private void AddErrors(string key, string[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                return;
            }

            this.errors ??= new Dictionary<string, List<string>>();

            if (!this.errors.TryGetValue(key: key, value: out var list))
            {
                list = new List<string>();
                this.errors.Add(key: key, value: list);
            }

            list.AddRange(collection: messages);
        }

        #endregion Private Methods
    }
}
