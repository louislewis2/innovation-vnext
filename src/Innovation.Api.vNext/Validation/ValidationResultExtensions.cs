namespace Innovation.Api.vNext.Validation
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Helper methods to convert between the classic <see cref="ValidationResult"/> shape and the
    /// <see cref="IDictionary{TKey, TValue}"/> error shape used by <see cref="IValidationResult.Errors"/> and
    /// <see cref="Commanding.ICommandResult"/>.
    /// </summary>
    public static class ValidationResultExtensions
    {
        #region Methods

        /// <summary>
        /// Converts a collection of <see cref="ValidationResult"/> into the dictionary error shape. A
        /// <see cref="ValidationResult"/> with multiple <see cref="ValidationResult.MemberNames"/> is fanned out
        /// into one dictionary entry per member name. Results with no member names are grouped under an empty
        /// key to represent a class/object level error.
        /// </summary>
        public static IDictionary<string, string[]> ToErrorDictionary(this IEnumerable<ValidationResult> validationResults)
        {
            if (validationResults == null)
            {
                return null;
            }

            var errors = new Dictionary<string, List<string>>();

            foreach (var validationResult in validationResults)
            {
                var hasMemberNames = false;

                foreach (var memberName in validationResult.MemberNames)
                {
                    hasMemberNames = true;

                    AddError(errors: errors, key: memberName, message: validationResult.ErrorMessage);
                }

                if (!hasMemberNames)
                {
                    AddError(errors: errors, key: string.Empty, message: validationResult.ErrorMessage);
                }
            }

            return errors.Count == 0
                ? null
                : errors.ToDictionary(keySelector: pair => pair.Key, elementSelector: pair => pair.Value.ToArray());
        }

        /// <summary>
        /// Converts the dictionary error shape back into a collection of <see cref="ValidationResult"/> - one per
        /// (key, message) pair. Note this is not a true inverse of <see cref="ToErrorDictionary"/>: if multiple
        /// member names originally shared a single <see cref="ValidationResult"/>, they will come back as separate
        /// instances since that grouping information isn't preserved in the dictionary shape.
        /// </summary>
        public static IEnumerable<ValidationResult> ToValidationResults(this IDictionary<string, string[]> errors)
        {
            if (errors == null)
            {
                yield break;
            }

            foreach (var errorEntry in errors)
            {
                var memberNames = string.IsNullOrEmpty(errorEntry.Key) ? Array.Empty<string>() : new[] { errorEntry.Key };

                foreach (var message in errorEntry.Value)
                {
                    yield return new ValidationResult(errorMessage: message, memberNames: memberNames);
                }
            }
        }

        #endregion Methods

        #region Private Methods

        private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
        {
            if (!errors.TryGetValue(key: key, value: out var list))
            {
                list = new List<string>();
                errors.Add(key: key, value: list);
            }

            list.Add(item: message ?? string.Empty);
        }

        #endregion Private Methods
    }
}
