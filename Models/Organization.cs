using System;
using System.Collections.Generic;

namespace InsuranceClaimManagement.Models
{
    public class Organization
    {
        public Organization()
        {
            StaffMembers = new List<ApplicationUser>();
            Customers = new List<User>();
            Claims = new List<Claim>();
            ClaimStatusHistory = new List<ClaimStatusHistory>();
            AuditEvents = new List<AuditEvent>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public ICollection<ApplicationUser> StaffMembers { get; set; }
        public ICollection<User> Customers { get; set; }
        public ICollection<Claim> Claims { get; set; }
        public ICollection<ClaimStatusHistory> ClaimStatusHistory { get; set; }
        public ICollection<AuditEvent> AuditEvents { get; set; }
    }
}
