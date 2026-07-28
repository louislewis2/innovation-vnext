namespace Innovation.Api.vNext.Reactions
{
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    using Commanding;

    public interface ICommandReactor<in TCommand> where TCommand : ICommand
    {
        Task React([DisallowNull] TCommand command);
    }
}
