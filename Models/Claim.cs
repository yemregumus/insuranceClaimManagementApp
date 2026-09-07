using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceClaimManagement.Models
{
    public class Claim
    {
        public Claim()
        {
            StatusHistory = new List<ClaimStatusHistory>();
            ConcurrencyToken = Guid.NewGuid().ToString();
        }

        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string ClaimType { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public int UserId { get; set; }
        public DateTime? ClaimDate { get; set; }
        public string PolicyNumber { get; set; }
        public string ConcurrencyToken { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedUtc { get; set; }
        public string DeletedByUserId { get; set; }
        public Organization Organization { get; set; }
        public User User { get; set; }
        public ICollection<ClaimStatusHistory> StatusHistory { get; set; }
    }
}
