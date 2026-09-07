using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using InsuranceClaimManagement.Security;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimManagement.Pages.Users
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentOrganization _currentOrganization;
        private readonly IAuditService _auditService;

        public CreateModel(
            ApplicationDbContext context,
            ICurrentOrganization currentOrganization,
            IAuditService auditService)
        {
            _context = context;
            _currentOrganization = currentOrganization;
            _auditService = auditService;
        }

        public void OnGet()
        {
        }

        [BindProperty]
        public CustomerInputModel Input { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input == null)
            {
                ModelState.AddModelError(string.Empty, "Customer details are required.");
            }
            else if (await _context.Customers.AnyAsync(customer => customer.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "A customer with this email already exists.");
            }

            if (Input != null && await _context.Customers.AnyAsync(customer => customer.Username == Input.Username))
            {
                ModelState.AddModelError("Input.Username", "A customer with this reference already exists.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Create a new User object
            var user = new User
            {
                OrganizationId = _currentOrganization.GetRequiredOrganizationId(),
                Name = Input.Name.Trim(),
                Email = Input.Email.Trim(),
                Username = Input.Username.Trim()
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Add the user to the context
            _context.Customers.Add(user);

            // Save changes to the database
            await _context.SaveChangesAsync();

            _auditService.Record(AuditEventTypes.Created, nameof(User), user.Id.ToString());
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Redirect to the Index page (or another page)
            return RedirectToPage("Index");
        }
    }
}
