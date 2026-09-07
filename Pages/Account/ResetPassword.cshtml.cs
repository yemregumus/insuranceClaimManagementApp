using System;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace InsuranceClaimManagement.Pages.Account
{
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public class ResetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public ResetPasswordInputModel Input { get; set; }

        public IActionResult OnGet(string userId, string code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
            {
                return NotFound();
            }

            Input = new ResetPasswordInputModel
            {
                UserId = userId,
                Code = code
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var staff = await _userManager.FindByIdAsync(Input.UserId);
            if (staff == null)
            {
                ModelState.AddModelError(string.Empty, "This password setup link is invalid or has expired.");
                return Page();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var resetResult = await _userManager.ResetPasswordAsync(staff, Input.Code, Input.Password);
            if (!resetResult.Succeeded)
            {
                AddIdentityErrors(resetResult);
                return Page();
            }

            staff.EmailConfirmed = true;
            staff.UpdatedUtc = DateTime.UtcNow;
            var updateResult = await _userManager.UpdateAsync(staff);
            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(updateResult);
                return Page();
            }

            _context.AuditEvents.Add(new AuditEvent
            {
                OrganizationId = staff.OrganizationId,
                ActorUserId = staff.Id,
                EventType = AuditEventTypes.PasswordSet,
                EntityType = nameof(ApplicationUser),
                EntityId = staff.Id,
                OccurredUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["StatusMessage"] = "Your password has been set. You can now sign in.";
            return RedirectToPage("Login");
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                var description = string.Equals(error.Code, "InvalidToken", StringComparison.Ordinal)
                    ? "This password setup link is invalid or has expired."
                    : error.Description;
                ModelState.AddModelError(string.Empty, description);
            }
        }
    }
}
