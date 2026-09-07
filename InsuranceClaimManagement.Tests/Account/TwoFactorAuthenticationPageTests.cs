using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using TwoFactorPage = InsuranceClaimManagement.Pages.Account.TwoFactorModel;

namespace InsuranceClaimManagement.Tests.Account
{
    public class TwoFactorAuthenticationPageTests
    {
        [Fact]
        public async Task SetupPageCreatesScannableQrCodeAndFormattedFallbackKey()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(staff);
            userManager.Setup(manager => manager.GetTwoFactorEnabledAsync(staff)).ReturnsAsync(false);
            userManager.Setup(manager => manager.CountRecoveryCodesAsync(staff)).ReturnsAsync(0);
            userManager.Setup(manager => manager.GetAuthenticatorKeyAsync(staff))
                .ReturnsAsync("ABCDEFGHIJKLMNOP");
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = CreatePage(context, userManager, signInManager, new RecordingAuditService());

            var result = await page.OnGetAsync();

            Assert.IsType<PageResult>(result);
            Assert.Equal("ABCD EFGH IJKL MNOP", page.SharedKey);
            Assert.Equal(staff.Email, page.AccountName);
            Assert.StartsWith("data:image/png;base64,", page.QrCodeDataUri);
            Assert.True(System.Convert.FromBase64String(page.QrCodeDataUri.Split(',')[1]).Length > 100);
        }

        [Fact]
        public async Task SetupPageCreatesMissingAuthenticatorKey()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(staff);
            userManager.Setup(manager => manager.GetTwoFactorEnabledAsync(staff)).ReturnsAsync(false);
            userManager.Setup(manager => manager.CountRecoveryCodesAsync(staff)).ReturnsAsync(0);
            userManager.SetupSequence(manager => manager.GetAuthenticatorKeyAsync(staff))
                .ReturnsAsync((string)null)
                .ReturnsAsync("ABCDEFGH");
            userManager.Setup(manager => manager.ResetAuthenticatorKeyAsync(staff)).ReturnsAsync(IdentityResult.Success);
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = CreatePage(context, userManager, signInManager, new RecordingAuditService());

            Assert.IsType<PageResult>(await page.OnGetAsync());
            Assert.Equal("ABCD EFGH", page.SharedKey);
            userManager.Verify(manager => manager.ResetAuthenticatorKeyAsync(staff), Times.Once);
            signInManager.Verify(manager => manager.RefreshSignInAsync(staff), Times.Once);
        }

        [Fact]
        public async Task SetupPageChallengesMissingUserAndReportsMissingEnabledKey()
        {
            await using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = CreatePage(context, userManager, signInManager, new RecordingAuditService());
            userManager.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null);
            Assert.IsType<ChallengeResult>(await page.OnGetAsync());

            var staff = Staff();
            userManager.Setup(manager => manager.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(staff);
            userManager.Setup(manager => manager.GetTwoFactorEnabledAsync(staff)).ReturnsAsync(true);
            userManager.Setup(manager => manager.CountRecoveryCodesAsync(staff)).ReturnsAsync(0);
            userManager.Setup(manager => manager.GetAuthenticatorKeyAsync(staff)).ReturnsAsync((string)null);
            Assert.IsType<PageResult>(await page.OnGetAsync());
            Assert.False(page.ModelState.IsValid);
        }

        [Fact]
        public async Task EnableRejectsInvalidCode()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff();
            var userManager = ConfiguredUserManager(staff);
            userManager.Setup(manager => manager.VerifyTwoFactorTokenAsync(
                    staff, It.IsAny<string>(), "123456"))
                .ReturnsAsync(false);
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = CreatePage(context, userManager, signInManager, new RecordingAuditService());
            page.Input = new EnableTwoFactorInputModel { VerificationCode = "123-456" };

            Assert.IsType<PageResult>(await page.OnPostEnableAsync());
            Assert.NotEmpty(page.ModelState["Input.VerificationCode"].Errors);
        }

        [Fact]
        public async Task EnableTurnsOnTwoFactorAndReturnsSingleUseRecoveryCodes()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff();
            var enabled = false;
            var userManager = ConfiguredUserManager(staff);
            userManager.Setup(manager => manager.GetTwoFactorEnabledAsync(staff))
                .ReturnsAsync(() => enabled);
            userManager.Setup(manager => manager.VerifyTwoFactorTokenAsync(
                    staff, It.IsAny<string>(), "123456"))
                .ReturnsAsync(true);
            userManager.Setup(manager => manager.SetTwoFactorEnabledAsync(staff, true))
                .Callback(() => enabled = true)
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(manager => manager.GenerateNewTwoFactorRecoveryCodesAsync(staff, 10))
                .ReturnsAsync(new[] { "recovery-one", "recovery-two" });
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var audit = new RecordingAuditService();
            var page = CreatePage(context, userManager, signInManager, audit);
            page.Input = new EnableTwoFactorInputModel { VerificationCode = "123 456" };

            Assert.IsType<PageResult>(await page.OnPostEnableAsync());
            Assert.True(page.IsEnabled);
            Assert.Equal(2, page.NewRecoveryCodes.Count);
            Assert.Contains(audit.Events, item => item.EventType == AuditEventTypes.TwoFactorEnabled);
            signInManager.Verify(manager => manager.RefreshSignInAsync(staff), Times.Once);
        }

        [Fact]
        public async Task RecoveryCodeGenerationRequiresEnabledTwoFactor()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff();
            var userManager = ConfiguredUserManager(staff);
            userManager.Setup(manager => manager.GetTwoFactorEnabledAsync(staff)).ReturnsAsync(false);
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = CreatePage(context, userManager, signInManager, new RecordingAuditService());

            Assert.IsType<BadRequestResult>(await page.OnPostRecoveryCodesAsync());
        }

        [Fact]
        public async Task RecoveryCodeGenerationReplacesCodesAndRecordsAudit()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff();
            var userManager = ConfiguredUserManager(staff);
            userManager.Setup(manager => manager.GetTwoFactorEnabledAsync(staff)).ReturnsAsync(true);
            userManager.Setup(manager => manager.GenerateNewTwoFactorRecoveryCodesAsync(staff, 10))
                .ReturnsAsync(new[] { "replacement-one", "replacement-two" });
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var audit = new RecordingAuditService();
            var page = CreatePage(context, userManager, signInManager, audit);

            Assert.IsType<PageResult>(await page.OnPostRecoveryCodesAsync());
            Assert.Equal(2, page.NewRecoveryCodes.Count);
            Assert.Contains(audit.Events, item => item.EventType == AuditEventTypes.RecoveryCodesGenerated);
        }

        private static Mock<UserManager<ApplicationUser>> ConfiguredUserManager(ApplicationUser staff)
        {
            var manager = TestInfrastructure.CreateUserManager();
            manager.Setup(item => item.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(staff);
            manager.Setup(item => item.GetTwoFactorEnabledAsync(staff)).ReturnsAsync(false);
            manager.Setup(item => item.CountRecoveryCodesAsync(staff)).ReturnsAsync(3);
            manager.Setup(item => item.GetAuthenticatorKeyAsync(staff)).ReturnsAsync("ABCDEFGHIJKLMNOP");
            return manager;
        }

        private static TwoFactorPage CreatePage(
            global::InsuranceClaimManagement.Data.ApplicationDbContext context,
            Mock<UserManager<ApplicationUser>> userManager,
            Mock<SignInManager<ApplicationUser>> signInManager,
            IAuditService auditService)
        {
            var page = new TwoFactorPage(
                context,
                userManager.Object,
                signInManager.Object,
                auditService,
                UrlEncoder.Default);
            TestInfrastructure.InitializePage(page);
            return page;
        }

        private static ApplicationUser Staff()
        {
            return new ApplicationUser
            {
                Id = TestInfrastructure.StaffUserId,
                OrganizationId = TestInfrastructure.OrganizationId,
                DisplayName = "Test Staff",
                Email = "staff+test@example.test",
                UserName = "staff+test@example.test",
                IsActive = true
            };
        }
    }
}
