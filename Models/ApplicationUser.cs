using System;
using Microsoft.AspNetCore.Identity;

namespace InsuranceClaimManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int OrganizationId { get; set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedUtc { get; set; }

        public Organization Organization { get; set; }
    }
}
