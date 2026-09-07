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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentOrganization _currentOrganization;
        private readonly IAuditService _auditService;

        // Constructor to inject the database context
        public CreateModel(
            ApplicationDbContext context,
            ICurrentOrganization currentOrganization,
            IAuditService auditService)
        {
            _context = context;
            _currentOrganization = currentOrganization;
            _auditService = auditService;
        }

        // GET handler
        [BindProperty]
        public ClaimInputModel Input { get; set; }

        public List<SelectListItem> CustomerOptions { get; set; }
        public List<SelectListItem> StatusOptions { get; set; }

        public async Task OnGetAsync()
        {
            Input = new ClaimInputModel
            {
                ClaimDate = DateTime.Today,
                Status = "Pending"
            };
            await LoadCustomerOptionsAsync();
            LoadStatusOptions();
        }

        public async Task<IActionResult> OnPostAsync()
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

            if (!ModelState.IsValid)
            {
                await LoadCustomerOptionsAsync();
                LoadStatusOptions();
                return Page();
            }

            // Create a new Claim object
            var claim = new Claim
            {
                OrganizationId = _currentOrganization.GetRequiredOrganizationId(),
                ClaimType = Input.ClaimType.Trim(),
                Description = Input.Description.Trim(),
                Amount = Input.Amount,
                Status = Input.Status,
                UserId = Input.UserId,
                ClaimDate = Input.ClaimDate,
                PolicyNumber = Input.PolicyNumber.Trim()
            };

            claim.StatusHistory.Add(new ClaimStatusHistory
            {
                OrganizationId = claim.OrganizationId,
                PreviousStatus = null,
                NewStatus = claim.Status,
                ChangedUtc = DateTime.UtcNow,
                ChangedByUserId = GetRequiredStaffUserId()
            });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.Claims.Add(claim);

            // Save changes to the database
            await _context.SaveChangesAsync();

            _auditService.Record(AuditEventTypes.Created, nameof(Claim), claim.Id.ToString());
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Redirect to the Index page
            return RedirectToPage("Index");
        }

        private async Task LoadCustomerOptionsAsync()
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
        }

        private void LoadStatusOptions()
        {
            StatusOptions = ClaimStatuses.All
                .Select(status => new SelectListItem(status, status))
                .ToList();
        }

        private string GetRequiredStaffUserId()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                   throw new InvalidOperationException("A signed-in staff account is required.");
        }
    }
}
