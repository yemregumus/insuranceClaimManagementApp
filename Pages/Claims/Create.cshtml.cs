using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;

namespace InsuranceClaimManagement.Pages.Claims
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        // Constructor to inject the database context
        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET handler
        public void OnGet()
        {
        }

        /*   public int Id { get; set; }
        public string ClaimType { get; set; }
        public string Description { get; set; }
        public DateTime ClaimDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public int UserId { get; set; }*/

        // POST handler
        public async Task<IActionResult> OnPostAsync(
            int Id, 
            string claimType, 
            string description, 
            string status,
            int userId,
            DateTime claimDate,
            string policyNumber
            )
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Create a new Claim object
            var claim = new Claim
            {
                Id = Id,
                ClaimType = claimType,
                Description = description,
                Status = status, 
                UserId = userId,
                ClaimDate = claimDate,
                PolicyNumber=policyNumber
            };

            // Add the claim to the context
            _context.Claims.Add(claim);

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Redirect to the Index page (or another page)
            return RedirectToPage("Index");
        }
    }
}
