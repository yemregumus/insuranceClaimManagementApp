using System;

namespace InsuranceClaimManagement.Models
{
    public class ClaimStatusHistory
    {
        public long Id { get; set; }
        public int OrganizationId { get; set; }
        public int ClaimId { get; set; }
        public string PreviousStatus { get; set; }
        public string NewStatus { get; set; }
        public DateTime ChangedUtc { get; set; }
        public string ChangedByUserId { get; set; }

        public Organization Organization { get; set; }
        public Claim Claim { get; set; }
        public ApplicationUser ChangedByUser { get; set; }
    }
}
