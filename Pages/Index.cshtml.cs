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

        public IList<User> Users { get; set; }
        public IList<Claim> Claims { get; set; }

        public async Task OnGetAsync()
        {
            Users = await _context.Users.ToListAsync();
            Claims = await _context.Claims.ToListAsync();
        }
    }
}
