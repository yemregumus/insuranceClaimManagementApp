using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace InsuranceClaimManagement.Pages.Claims
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // Add this property to store the list of claims
        public List<Claim> Claims { get; set; }

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            // Fetch the claims from the database and assign them to the Claims property
            Claims = _context.Claims.ToList();
        }
    }
}
