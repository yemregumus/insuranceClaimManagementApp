using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceClaimManagement.Models
{
    public class Claim
    {
        public int Id { get; set; }
        public string ClaimType { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public int UserId { get; set; }
        public DateTime ClaimDate { get; set; }
        public string PolicyNumber { get; set; }

    }
}
