using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceClaimManagement.Models
{
    public class User
    {
        public User()
        {
            Claims = new List<Claim>();
        }

        public int Id { get; set; } 
        public int OrganizationId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }

        public Organization Organization { get; set; }
        public ICollection<Claim> Claims { get; set; }
    }
}
