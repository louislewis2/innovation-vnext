namespace Innovation.Api.vNext.Reactions
{
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    using Commanding;

    public interface ICommandResultReactor<in TCommand> where TCommand : ICommand
    {
        Task React([DisallowNull] ICommandResult commandResult, [DisallowNull] TCommand command);
    }
}
