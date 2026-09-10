namespace Innovation.Sample.Data.Stores
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Innovation.Api.vNext.Querying;
    using Innovation.Api.vNext.Messaging;
    using Innovation.Api.vNext.Commanding;
    using Innovation.Api.vNext.Dispatching;

    using Innovation.Sample.Data.Contexts;
    using Innovation.Sample.Data.Anemics.Auditing;
    using Innovation.Sample.Infrastructure.Settings;

    public class InnovationAuditStore<TAuditDbContext> : IAuditStore where TAuditDbContext : AuditDbContextBase<TAuditDbContext>
    {
        #region Fields

        private readonly ILogger logger;
        private readonly AuditDbContextBase<TAuditDbContext> auditDbContext;
        private readonly InnovationAuditSettings innovationAuditSettings;

        #endregion Fields

        #region Constructor

        public InnovationAuditStore(
            ILogger<InnovationAuditStore<TAuditDbContext>> logger,
            AuditDbContextBase<TAuditDbContext> auditDbContext,
            IOptions<InnovationAuditSettings> innovationAuditSettingsOptions)
        {
            this.logger = logger;
            this.auditDbContext = auditDbContext;
            this.innovationAuditSettings = innovationAuditSettingsOptions.Value;
        }

        #endregion Constructor

        #region Methods

        public async Task Log(AuditContext auditContext, ICommand command, ICommandResult commandResult, CancellationToken cancellationToken)
        {
            if (!this.innovationAuditSettings.EnableCommandAudits)
            {
                return;
            }

            await this.InsertCommandAudit(
                auditContext: auditContext,
                command: command,
                commandResult: commandResult,
                cancellationToken: cancellationToken);
        }

        public async Task Log(AuditContext auditContext, IQuery query, CancellationToken cancellationToken)
        {
            if (!this.innovationAuditSettings.EnableQueryAudits)
            {
                return;
            }

            await this.InsertQueryAudit(
                auditContext: auditContext,
                query: query,
                cancellationToken: cancellationToken);
        }

        public Task Log(AuditContext auditContext, IMessage message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        #endregion Methods

        #region Private Methods

        private async Task InsertCommandAudit(AuditContext auditContext, ICommand command, ICommandResult commandResult, CancellationToken cancellationToken)
        {
            try
            {
                var commandContext = JsonSerializer.Serialize(value: command);
                var commandResultContext = JsonSerializer.Serialize(value: commandResult);

                var commandAuditEntryAnemic = CommandAuditEntryAnemic.New(
                    id: Guid.NewGuid(),
                    name: command.EventName,
                    commandContext: commandContext,
                    commandResultContext: commandResultContext,
                    correlationId: auditContext.CorrelationId,
                    runtimeMilliSeconds: auditContext.RuntimeMilliSeconds);

                this.auditDbContext.Add(entity: commandAuditEntryAnemic);
                await this.auditDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(exception: ex, message: ex.GetInnerMostMessage());
            }
        }

        private async Task InsertQueryAudit(AuditContext auditContext, IQuery query, CancellationToken cancellationToken)
        {
            try
            {
                var queryContext = JsonSerializer.Serialize(value: query);

                var queryAuditEntryAnemic = QueryAuditEntryAnemic.New(
                    id: Guid.NewGuid(),
                    name: query.EventName,
                    queryContext: queryContext,
                    correlationId: auditContext.CorrelationId,
                    runtimeMilliSeconds: auditContext.RuntimeMilliSeconds);

                this.auditDbContext.Add(entity: queryAuditEntryAnemic);
                await this.auditDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(exception: ex, message: ex.GetInnerMostMessage());
            }
        }

        #endregion Private Methods
    }
}
