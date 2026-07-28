namespace Innovation.Api.vNext.Dispatching
{
    using System.Threading.Tasks;
    using System.Diagnostics.CodeAnalysis;

    using Querying;
    using Messaging;
    using Commanding;

    public interface IAuditStore
    {
        Task Log([DisallowNull] AuditContext auditContext, [DisallowNull] ICommand command, [DisallowNull] ICommandResult commandResult);
        Task Log([DisallowNull] AuditContext auditContext, [DisallowNull] IQuery query);
        Task Log([DisallowNull] AuditContext auditContext, [DisallowNull] IMessage message);
    }
}
