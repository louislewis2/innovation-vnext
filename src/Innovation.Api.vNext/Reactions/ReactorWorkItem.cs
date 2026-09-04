namespace Innovation.Api.vNext.Reactions
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Commanding;

    /// <summary>
    /// Represents a unit of reactor work to be executed later by an <see cref="IReactorWorkQueue"/>.
    /// This is plain data, not a closure - it carries enough information for a queue's invoker to
    /// resolve and invoke the correct reactor(s) at a later point, potentially in a different scope, or
    /// (for a custom durable <see cref="IReactorWorkQueue"/>) even in a different process after a restart.
    /// A closure/delegate could never support that, since it only exists as compiled code plus captured
    /// object references in the current process's memory.
    /// </summary>
    public sealed class ReactorWorkItem
    {
        #region Constructor

        public ReactorWorkItem(
            string correlationId,
            [DisallowNull] Type reactorContractType,
            [DisallowNull] ICommand command,
            ICommandResult commandResult)
        {
            this.CorrelationId = correlationId;
            this.ReactorContractType = reactorContractType ?? throw new ArgumentNullException(paramName: nameof(reactorContractType));
            this.Command = command ?? throw new ArgumentNullException(paramName: nameof(command));
            this.CommandResult = commandResult;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// The correlation id of the original dispatch that produced this work item.
        /// </summary>
        public string CorrelationId { get; }

        /// <summary>
        /// The closed generic reactor contract to resolve and invoke,
        /// e.g. typeof(ICommandResultReactor&lt;InsertVendorCommand&gt;).
        /// </summary>
        public Type ReactorContractType { get; }

        /// <summary>
        /// The command that was dispatched.
        /// </summary>
        public ICommand Command { get; }

        /// <summary>
        /// The result of the command, if known at the time this work item was created. Null for
        /// ICommandReactor work items (which fire before the result is known). Populated for
        /// ICommandResultReactor work items.
        /// </summary>
        public ICommandResult CommandResult { get; }

        #endregion Properties
    }
}
