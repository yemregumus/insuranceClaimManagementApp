using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimManagement.Pages.Claims
{
    [Authorize(Policy = PolicyNames.ClaimsManage)]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public DeleteModel(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public Claim ClaimRecord { get; set; }

        [BindProperty]
        public string ConcurrencyToken { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ClaimRecord = await _context.Claims
                .AsNoTracking()
                .Include(claim => claim.User)
                .SingleOrDefaultAsync(claim => claim.Id == id);

            if (ClaimRecord == null)
            {
                return NotFound();
            }
            ConcurrencyToken = ClaimRecord.ConcurrencyToken;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var claim = await _context.Claims.SingleOrDefaultAsync(item => item.Id == id);
            if (claim == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(ConcurrencyToken))
            {
                return BadRequest();
            }

            _context.Entry(claim)
                .Property(item => item.ConcurrencyToken)
                .OriginalValue = ConcurrencyToken;
            claim.IsDeleted = true;
            claim.DeletedUtc = DateTime.UtcNow;
            claim.DeletedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                throw new InvalidOperationException("A signed-in staff account is required.");
            claim.ConcurrencyToken = Guid.NewGuid().ToString();
            _auditService.Record(AuditEventTypes.Archived, nameof(Claim), claim.Id.ToString());

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty,
                    "Another staff member changed this claim. Return to the claims list and try again.");
                ClaimRecord = await _context.Claims
                    .AsNoTracking()
                    .Include(item => item.User)
                    .SingleOrDefaultAsync(item => item.Id == id);
                if (ClaimRecord == null)
                {
                    return NotFound();
                }

                ConcurrencyToken = ClaimRecord.ConcurrencyToken;
                ModelState.Remove(nameof(ConcurrencyToken));
                return Page();
            }

            return RedirectToPage("Index");
        }
    }
}
