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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public EditModel(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [BindProperty]
        public CustomerInputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var customer = await _context.Customers
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            Input = new CustomerInputModel
            {
                Name = customer.Name,
                Email = customer.Email,
                Username = customer.Username
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (Input == null)
            {
                ModelState.AddModelError(string.Empty, "Customer details are required.");
            }
            else if (await _context.Customers.AnyAsync(customer => customer.Id != id && customer.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "A customer with this email already exists.");
            }

            if (Input != null && await _context.Customers.AnyAsync(customer => customer.Id != id && customer.Username == Input.Username))
            {
                ModelState.AddModelError("Input.Username", "A customer with this reference already exists.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var customer = await _context.Customers.SingleOrDefaultAsync(item => item.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.Name = Input.Name.Trim();
            customer.Email = Input.Email.Trim();
            customer.Username = Input.Username.Trim();

            _auditService.Record(AuditEventTypes.Updated, nameof(User), customer.Id.ToString());

            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}
