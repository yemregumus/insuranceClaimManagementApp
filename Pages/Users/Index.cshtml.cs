using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

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

        public void OnGet()
        {
            // Fetch the users from the database and assign them to the Users property
            Users = _context.Users.ToList();
        }
    }
}
