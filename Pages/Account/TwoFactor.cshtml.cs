using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;

namespace InsuranceClaimManagement.Pages.Account
{
    [Authorize]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class TwoFactorModel : PageModel
    {
        private const string AuthenticatorIssuer = "Insurance Claim Management";
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuditService _auditService;
        private readonly UrlEncoder _urlEncoder;

        public TwoFactorModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuditService auditService,
            UrlEncoder urlEncoder)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _auditService = auditService;
            _urlEncoder = urlEncoder;
        }

        [BindProperty]
        public EnableTwoFactorInputModel Input { get; set; }

        public bool IsEnabled { get; private set; }
        public string SharedKey { get; private set; }
        public string AccountName { get; private set; }
        public string QrCodeDataUri { get; private set; }
        public int RecoveryCodesRemaining { get; private set; }
        public IReadOnlyList<string> NewRecoveryCodes { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var staff = await _userManager.GetUserAsync(User);
            if (staff == null)
            {
                return Challenge();
            }

            await LoadAsync(staff);
            return Page();
        }

        public async Task<IActionResult> OnPostEnableAsync()
        {
            var staff = await _userManager.GetUserAsync(User);
            if (staff == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(staff);
                return Page();
            }

            var code = Input.VerificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                staff,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                code);

            if (!isValid)
            {
                ModelState.AddModelError("Input.VerificationCode", "The verification code is invalid.");
                await LoadAsync(staff);
                return Page();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var enableResult = await _userManager.SetTwoFactorEnabledAsync(staff, true);
            if (!enableResult.Succeeded)
            {
                AddIdentityErrors(enableResult);
                await LoadAsync(staff);
                return Page();
            }

            NewRecoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(staff, 10)).ToList();
            _auditService.Record(AuditEventTypes.TwoFactorEnabled, nameof(ApplicationUser), staff.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await _signInManager.RefreshSignInAsync(staff);
            await LoadAsync(staff, keepRecoveryCodes: true);
            return Page();
        }

        public async Task<IActionResult> OnPostRecoveryCodesAsync()
        {
            var staff = await _userManager.GetUserAsync(User);
            if (staff == null)
            {
                return Challenge();
            }

            if (!await _userManager.GetTwoFactorEnabledAsync(staff))
            {
                return BadRequest();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            NewRecoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(staff, 10)).ToList();
            _auditService.Record(AuditEventTypes.RecoveryCodesGenerated, nameof(ApplicationUser), staff.Id);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            await LoadAsync(staff, keepRecoveryCodes: true);
            return Page();
        }

        private async Task LoadAsync(ApplicationUser staff, bool keepRecoveryCodes = false)
        {
            IsEnabled = await _userManager.GetTwoFactorEnabledAsync(staff);
            AccountName = staff.Email ?? staff.UserName ?? "staff";
            RecoveryCodesRemaining = await _userManager.CountRecoveryCodesAsync(staff);

            var key = await _userManager.GetAuthenticatorKeyAsync(staff);
            if (string.IsNullOrWhiteSpace(key))
            {
                if (IsEnabled)
                {
                    ModelState.AddModelError(string.Empty,
                        "The authenticator key is unavailable. Ask an administrator to reset two-factor authentication.");
                    return;
                }

                var resetResult = await _userManager.ResetAuthenticatorKeyAsync(staff);
                if (!resetResult.Succeeded)
                {
                    AddIdentityErrors(resetResult);
                    return;
                }

                key = await _userManager.GetAuthenticatorKeyAsync(staff);
                await _signInManager.RefreshSignInAsync(staff);
            }

            SharedKey = FormatKey(key);
            QrCodeDataUri = GenerateQrCodeDataUri(AccountName, key);
            if (!keepRecoveryCodes)
            {
                NewRecoveryCodes = null;
            }
        }

        private string GenerateQrCodeDataUri(string accountName, string unformattedKey)
        {
            var authenticatorUri = string.Format(
                CultureInfo.InvariantCulture,
                AuthenticatorUriFormat,
                _urlEncoder.Encode(AuthenticatorIssuer),
                _urlEncoder.Encode(accountName),
                _urlEncoder.Encode(unformattedKey));

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(8);

            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }

        private static string FormatKey(string unformattedKey)
        {
            return string.Join(" ", Enumerable.Range(0, (unformattedKey.Length + 3) / 4)
                .Select(index => unformattedKey.Substring(
                    index * 4,
                    Math.Min(4, unformattedKey.Length - index * 4))));
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
