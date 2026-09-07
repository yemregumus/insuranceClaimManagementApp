using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICurrentOrganization _currentOrganization;
        private readonly IAuditService _auditService;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ICurrentOrganization currentOrganization,
            IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _currentOrganization = currentOrganization;
            _auditService = auditService;
        }

        [BindProperty]
        public StaffCreateInputModel Input { get; set; }

        public List<SelectListItem> RoleOptions { get; private set; }
        public bool AccountCreated { get; private set; }
        public string PasswordSetupUrl { get; private set; }

        public void OnGet()
        {
            LoadRoleOptions();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LoadRoleOptions();

            if (Input == null)
            {
                ModelState.AddModelError(string.Empty, "Staff account details are required.");
            }
            else if (!RoleNames.IsAssignable(Input.Role) || !await _roleManager.RoleExistsAsync(Input.Role))
            {
                ModelState.AddModelError("Input.Role", "Select a valid staff role.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim();
            var staff = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                DisplayName = Input.DisplayName.Trim(),
                OrganizationId = _currentOrganization.GetRequiredOrganizationId(),
                IsActive = true,
                CreatedUtc = DateTime.UtcNow
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var internalPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) + "aA1!";
            var createResult = await _userManager.CreateAsync(staff, internalPassword);
            if (!createResult.Succeeded)
            {
                AddIdentityErrors(createResult);
                return Page();
            }

            var roleResult = await _userManager.AddToRoleAsync(staff, Input.Role);
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);
                return Page();
            }

            _auditService.Record(AuditEventTypes.StaffCreated, nameof(ApplicationUser), staff.Id);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var code = await _userManager.GeneratePasswordResetTokenAsync(staff);
            PasswordSetupUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { userId = staff.Id, code },
                protocol: Request.Scheme);
            AccountCreated = true;
            return Page();
        }

        private void LoadRoleOptions()
        {
            RoleOptions = RoleNames.AssignableRoles
                .Select(role => new SelectListItem(role, role))
                .ToList();
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
