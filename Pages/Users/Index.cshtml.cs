using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InsuranceClaimManagement.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // Add this property to store the list of users
        public List<User> Users { get; set; }

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            Users = await _context.Customers
                .AsNoTracking()
                .OrderBy(customer => customer.Name)
                .ToListAsync();
        }
    }
}
