namespace Innovation.Api.vNext.Core
{
    public interface ICorrelationAware
    {
        string CorrelationId { set; }
    }
}
