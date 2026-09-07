namespace InsuranceClaimManagement.Security
{
    public interface IAuditService
    {
        void Record(string eventType, string entityType, string entityId);
    }
}
