namespace Innovation.ServiceBus.InProcess.vNext.Validators
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using MiniValidation;

    public class DataAnnotationsValidator
    {
        #region Fields

        private readonly IServiceProvider serviceProvider;

        #endregion Fields

        #region Constructor

        public DataAnnotationsValidator(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        #endregion Constructor

        #region Methods

        public async ValueTask<(bool isValid, IDictionary<string, string[]> Errors)> TryValidateObject<TTarget>(TTarget target)
        {
            return await MiniValidator.TryValidateAsync(target: target, serviceProvider: this.serviceProvider, recurse: false);
        }

        public async ValueTask<(bool isValid, IDictionary<string, string[]> Errors)> TryValidateObjectRecursive<TTarget>(TTarget target)
        {
            return await MiniValidator.TryValidateAsync(target: target, serviceProvider: this.serviceProvider, recurse: true);
        }

        #endregion Methods
    }
}
