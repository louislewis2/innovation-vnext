namespace Innovation.Api.vNext.Interceptors
{
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    using Commanding;

    public interface ICommandInterceptor<in TCommand> where TCommand : ICommand
    {
        ValueTask Intercept([DisallowNull] TCommand command);
    }
}
