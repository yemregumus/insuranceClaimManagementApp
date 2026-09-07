using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Security;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimManagement.Pages.Users
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public DeleteModel(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public User Customer { get; set; }
        public int RelatedClaimCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Customer = await _context.Customers
                .AsNoTracking()
                .SingleOrDefaultAsync(customer => customer.Id == id);

            if (Customer == null)
            {
                return NotFound();
            }

            RelatedClaimCount = await _context.Claims
                .IgnoreQueryFilters()
                .CountAsync(claim =>
                    claim.OrganizationId == Customer.OrganizationId && claim.UserId == id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var customer = await _context.Customers.SingleOrDefaultAsync(item => item.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            if (await _context.Claims
                .IgnoreQueryFilters()
                .AnyAsync(claim =>
                    claim.OrganizationId == customer.OrganizationId && claim.UserId == id))
            {
                ModelState.AddModelError(string.Empty, "This customer cannot be deleted while claims are associated with it.");
                Customer = customer;
                RelatedClaimCount = await _context.Claims
                    .IgnoreQueryFilters()
                    .CountAsync(claim =>
                        claim.OrganizationId == customer.OrganizationId && claim.UserId == id);
                return Page();
            }

            _auditService.Record(AuditEventTypes.Archived, nameof(User), customer.Id.ToString());
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}
