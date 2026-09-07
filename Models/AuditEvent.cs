using System;

namespace InsuranceClaimManagement.Models
{
    public class AuditEvent
    {
        public long Id { get; set; }
        public int OrganizationId { get; set; }
        public string ActorUserId { get; set; }
        public string EventType { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public DateTime OccurredUtc { get; set; }

        public Organization Organization { get; set; }
        public ApplicationUser ActorUser { get; set; }
    }
}
