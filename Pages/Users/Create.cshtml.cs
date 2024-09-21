using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;

namespace InsuranceClaimManagement.Pages.Users
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string name, string email, string username, string password)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Create a new User object
            var user = new User
            {
                Name = name,
                Email = email,
                Username = username,
                Password = password 
            };

            // Add the user to the context
            _context.Users.Add(user);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Redirect to the Index page (or another page)
            return RedirectToPage("Index");
        }
    }
}
