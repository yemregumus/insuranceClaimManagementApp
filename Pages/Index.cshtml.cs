using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimManagement.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int CustomerCount { get; set; }
        public int ClaimCount { get; set; }
        public int PendingClaimCount { get; set; }

        public async Task OnGetAsync()
        {
            CustomerCount = await _context.Customers.CountAsync();
            ClaimCount = await _context.Claims.CountAsync();
            PendingClaimCount = await _context.Claims.CountAsync(claim => claim.Status == "Pending");
        }
    }
}
