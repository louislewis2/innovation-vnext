namespace Innovation.Api.vNext.Messaging
{
    using System.Diagnostics.CodeAnalysis;

    public interface IAddressable
    {
        [DisallowNull] string[] Handles { get; }
    }
}
