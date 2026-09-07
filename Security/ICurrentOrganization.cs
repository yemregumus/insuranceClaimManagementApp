namespace InsuranceClaimManagement.Security
{
    public interface ICurrentOrganization
    {
        int? OrganizationId { get; }
        int GetRequiredOrganizationId();
    }
}
