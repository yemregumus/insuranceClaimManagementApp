using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task OnGetAsync()
        {
            Claims = await _context.Claims
                .AsNoTracking()
                .Include(claim => claim.User)
                .OrderByDescending(claim => claim.ClaimDate)
                .ThenByDescending(claim => claim.Id)
                .ToListAsync();
        }
    }
}
