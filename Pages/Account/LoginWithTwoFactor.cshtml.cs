using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimManagement.Pages.Account
{
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public class LoginWithTwoFactorModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public LoginWithTwoFactorModel(
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _context = context;
        }

        [BindProperty]
        public TwoFactorLoginInputModel Input { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool RememberMe { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Page("/Index");
            return await ValidatePendingAccountAsync() ? Page() : RedirectToPage("Login");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Page("/Index");

            if (!await ValidatePendingAccountAsync())
            {
                return RedirectToPage("Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var code = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                code,
                RememberMe,
                Input.RememberMachine);

            if (result.Succeeded)
            {
                return LocalRedirect(ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is temporarily locked. Try again later.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
            return Page();
        }

        private async Task<bool> ValidatePendingAccountAsync()
        {
            var staff = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            return staff != null &&
                   staff.IsActive &&
                   await _context.Organizations
                       .AsNoTracking()
                       .AnyAsync(organization =>
                           organization.Id == staff.OrganizationId && organization.IsActive);
        }
    }
}
