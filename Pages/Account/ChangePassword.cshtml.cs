using System.Threading.Tasks;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InsuranceClaimManagement.Pages.Account
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public ChangePasswordInputModel Input { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var staff = await _userManager.GetUserAsync(User);
            if (staff == null)
            {
                return Challenge();
            }

            var result = await _userManager.ChangePasswordAsync(
                staff,
                Input.CurrentPassword,
                Input.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

            await _signInManager.RefreshSignInAsync(staff);
            TempData["StatusMessage"] = "Your password has been changed.";
            return RedirectToPage("/Index");
        }
    }
}
