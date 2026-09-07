using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace InsuranceClaimManagement.Pages.Claims
{
    [Authorize(Policy = PolicyNames.ClaimsManage)]
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
        public ClaimInputModel Input { get; set; }

        public List<SelectListItem> CustomerOptions { get; set; }
        public List<SelectListItem> StatusOptions { get; set; }
        public List<ClaimStatusHistory> StatusHistory { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var claim = await _context.Claims
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id);

            if (claim == null)
            {
                return NotFound();
            }

            Input = new ClaimInputModel
            {
                ClaimType = claim.ClaimType,
                Description = claim.Description,
                Amount = claim.Amount,
                Status = claim.Status,
                UserId = claim.UserId,
                ClaimDate = claim.ClaimDate,
                PolicyNumber = claim.PolicyNumber,
                ConcurrencyToken = claim.ConcurrencyToken
            };

            await LoadSupportingDataAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (Input == null)
            {
                ModelState.AddModelError(string.Empty, "Claim details are required.");
            }
            else if (!await _context.Customers.AnyAsync(customer => customer.Id == Input.UserId))
            {
                ModelState.AddModelError("Input.UserId", "Select an existing customer.");
            }

            if (Input != null && !ClaimStatuses.IsValid(Input.Status))
            {
                ModelState.AddModelError("Input.Status", "Select a valid claim status.");
            }

            if (Input != null && string.IsNullOrWhiteSpace(Input.ConcurrencyToken))
            {
                ModelState.AddModelError(string.Empty, "The claim version is missing. Reload the page and try again.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSupportingDataAsync(id);
                return Page();
            }

            var claim = await _context.Claims.SingleOrDefaultAsync(item => item.Id == id);
            if (claim == null)
            {
                return NotFound();
            }

            var previousStatus = claim.Status;
            _context.Entry(claim)
                .Property(item => item.ConcurrencyToken)
                .OriginalValue = Input.ConcurrencyToken;

            claim.ClaimType = Input.ClaimType.Trim();
            claim.Description = Input.Description.Trim();
            claim.Amount = Input.Amount;
            claim.Status = Input.Status;
            claim.UserId = Input.UserId;
            claim.ClaimDate = Input.ClaimDate;
            claim.PolicyNumber = Input.PolicyNumber.Trim();
            claim.ConcurrencyToken = Guid.NewGuid().ToString();

            if (!string.Equals(previousStatus, claim.Status, StringComparison.Ordinal))
            {
                _context.ClaimStatusHistories.Add(new ClaimStatusHistory
                {
                    OrganizationId = claim.OrganizationId,
                    ClaimId = claim.Id,
                    PreviousStatus = previousStatus,
                    NewStatus = claim.Status,
                    ChangedUtc = DateTime.UtcNow,
                    ChangedByUserId = GetRequiredStaffUserId()
                });
            }

            _auditService.Record(AuditEventTypes.Updated, nameof(Claim), claim.Id.ToString());

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                _context.Entry(claim).State = EntityState.Detached;
                var currentClaim = await _context.Claims
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == id);
                if (currentClaim == null)
                {
                    return NotFound();
                }

                Input.ConcurrencyToken = currentClaim.ConcurrencyToken;
                ModelState.Remove("Input.ConcurrencyToken");
                ModelState.AddModelError(string.Empty,
                    "Another staff member changed this claim. Review your values and submit again.");
                await LoadSupportingDataAsync(id);
                return Page();
            }

            return RedirectToPage("Index");
        }

        private async Task LoadSupportingDataAsync(int claimId)
        {
            CustomerOptions = await _context.Customers
                .AsNoTracking()
                .OrderBy(customer => customer.Name)
                .Select(customer => new SelectListItem
                {
                    Value = customer.Id.ToString(),
                    Text = customer.Name + " (" + customer.Username + ")"
                })
                .ToListAsync();

            StatusOptions = ClaimStatuses.All
                .Select(status => new SelectListItem(status, status))
                .ToList();

            StatusHistory = await _context.ClaimStatusHistories
                .AsNoTracking()
                .Where(history => history.ClaimId == claimId)
                .Include(history => history.ChangedByUser)
                .OrderByDescending(history => history.ChangedUtc)
                .ToListAsync();
        }

        private string GetRequiredStaffUserId()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                   throw new InvalidOperationException("A signed-in staff account is required.");
        }
    }
}
