using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace InsuranceClaimManagement.Pages.Account
{
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Page("/Index");
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Page("/Index");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var staff = await _userManager.FindByEmailAsync(Input.Email.Trim());
            var organizationIsActive = staff != null &&
                await _context.Organizations
                    .AsNoTracking()
                    .AnyAsync(organization => organization.Id == staff.OrganizationId && organization.IsActive);

            if (staff == null || !staff.IsActive || !organizationIsActive)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                staff,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return LocalRedirect(ReturnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage(
                    "/Account/LoginWithTwoFactor",
                    new { returnUrl = ReturnUrl, rememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account is temporarily locked. Try again later.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }
    }
}
