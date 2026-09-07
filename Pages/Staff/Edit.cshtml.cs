using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimManagement.Pages.Staff
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ICurrentOrganization _currentOrganization;
        private readonly IAuditService _auditService;

        public EditModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ICurrentOrganization currentOrganization,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _currentOrganization = currentOrganization;
            _auditService = auditService;
        }

        [BindProperty]
        public StaffEditInputModel Input { get; set; }

        public string Email { get; private set; }
        public List<SelectListItem> RoleOptions { get; private set; }
        public string PasswordSetupUrl { get; private set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            var staff = await FindStaffAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(staff);
            Email = staff.Email;
            Input = new StaffEditInputModel
            {
                DisplayName = staff.DisplayName,
                IsActive = staff.IsActive,
                Role = currentRoles.FirstOrDefault(RoleNames.IsAssignable)
            };
            LoadRoleOptions();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            var staff = await FindStaffAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            Email = staff.Email;
            LoadRoleOptions();

            if (Input == null)
            {
                ModelState.AddModelError(string.Empty, "Staff account details are required.");
            }
            else if (!RoleNames.IsAssignable(Input.Role))
            {
                ModelState.AddModelError("Input.Role", "Select a valid staff role.");
            }

            var signedInUserId = _userManager.GetUserId(User);
            if (Input != null && string.Equals(staff.Id, signedInUserId, StringComparison.Ordinal) &&
                (!Input.IsActive || !string.Equals(Input.Role, RoleNames.Administrator, StringComparison.Ordinal)))
            {
                ModelState.AddModelError(string.Empty,
                    "You cannot deactivate your own account or remove your own administrator role.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            staff.DisplayName = Input.DisplayName.Trim();
            staff.IsActive = Input.IsActive;
            staff.UpdatedUtc = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(staff);
            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(updateResult);
                return Page();
            }

            var currentRoles = await _userManager.GetRolesAsync(staff);
            var assignableCurrentRoles = currentRoles.Where(RoleNames.IsAssignable).ToList();
            var rolesToRemove = assignableCurrentRoles
                .Where(role => !string.Equals(role, Input.Role, StringComparison.Ordinal))
                .ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(staff, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    AddIdentityErrors(removeResult);
                    return Page();
                }
            }

            if (!currentRoles.Contains(Input.Role, StringComparer.Ordinal))
            {
                var addResult = await _userManager.AddToRoleAsync(staff, Input.Role);
                if (!addResult.Succeeded)
                {
                    AddIdentityErrors(addResult);
                    return Page();
                }
            }

            var stampResult = await _userManager.UpdateSecurityStampAsync(staff);
            if (!stampResult.Succeeded)
            {
                AddIdentityErrors(stampResult);
                return Page();
            }

            _auditService.Record(AuditEventTypes.StaffUpdated, nameof(ApplicationUser), staff.Id);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            TempData["StatusMessage"] = "Staff account updated.";
            return RedirectToPage("Index");
        }

        public async Task<IActionResult> OnPostPasswordSetupAsync(string id)
        {
            var staff = await FindStaffAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(staff);
            PasswordSetupUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { userId = staff.Id, code },
                protocol: Request.Scheme);

            _auditService.Record(
                AuditEventTypes.PasswordSetupLinkCreated,
                nameof(ApplicationUser),
                staff.Id);
            await _context.SaveChangesAsync();

            var currentRoles = await _userManager.GetRolesAsync(staff);
            Email = staff.Email;
            Input = new StaffEditInputModel
            {
                DisplayName = staff.DisplayName,
                IsActive = staff.IsActive,
                Role = currentRoles.FirstOrDefault(RoleNames.IsAssignable)
            };
            LoadRoleOptions();
            return Page();
        }

        public async Task<IActionResult> OnPostResetTwoFactorAsync(string id)
        {
            var staff = await FindStaffAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var disableResult = await _userManager.SetTwoFactorEnabledAsync(staff, false);
            if (!disableResult.Succeeded)
            {
                AddIdentityErrors(disableResult);
                await LoadForDisplayAsync(staff);
                return Page();
            }

            var resetResult = await _userManager.ResetAuthenticatorKeyAsync(staff);
            if (!resetResult.Succeeded)
            {
                AddIdentityErrors(resetResult);
                await LoadForDisplayAsync(staff);
                return Page();
            }

            _auditService.Record(AuditEventTypes.TwoFactorReset, nameof(ApplicationUser), staff.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (string.Equals(staff.Id, _userManager.GetUserId(User), StringComparison.Ordinal))
            {
                await _signInManager.RefreshSignInAsync(staff);
            }

            TempData["StatusMessage"] = "Two-factor authentication was reset.";
            return RedirectToPage("Index");
        }

        private Task<ApplicationUser> FindStaffAsync(string id)
        {
            var organizationId = _currentOrganization.GetRequiredOrganizationId();
            return _context.Users.SingleOrDefaultAsync(staff =>
                staff.Id == id && staff.OrganizationId == organizationId);
        }

        private void LoadRoleOptions()
        {
            RoleOptions = RoleNames.AssignableRoles
                .Select(role => new SelectListItem(role, role))
                .ToList();
        }

        private async Task LoadForDisplayAsync(ApplicationUser staff)
        {
            var currentRoles = await _userManager.GetRolesAsync(staff);
            Email = staff.Email;
            Input = new StaffEditInputModel
            {
                DisplayName = staff.DisplayName,
                IsActive = staff.IsActive,
                Role = currentRoles.FirstOrDefault(RoleNames.IsAssignable)
            };
            LoadRoleOptions();
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
