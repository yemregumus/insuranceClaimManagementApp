using System;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ChangePasswordPage = InsuranceClaimManagement.Pages.Account.ChangePasswordModel;
using LoginPage = InsuranceClaimManagement.Pages.Account.LoginModel;
using LogoutPage = InsuranceClaimManagement.Pages.Account.LogoutModel;
using RecoveryLoginPage = InsuranceClaimManagement.Pages.Account.LoginWithRecoveryCodeModel;
using ResetPasswordPage = InsuranceClaimManagement.Pages.Account.ResetPasswordModel;
using TwoFactorLoginPage = InsuranceClaimManagement.Pages.Account.LoginWithTwoFactorModel;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace InsuranceClaimManagement.Tests.Account
{
    public class AuthenticationPageTests
    {
        [Fact]
        public void LoginGetAcceptsOnlyLocalReturnUrls()
        {
            using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = new LoginPage(signInManager.Object, userManager.Object, context);
            TestInfrastructure.InitializePage(page);

            page.OnGet("https://attacker.example");
            Assert.Equal("/generated", page.ReturnUrl);

            page.OnGet("/Claims");
            Assert.Equal("/Claims", page.ReturnUrl);
        }

        [Fact]
        public async Task LoginRejectsInvalidModelAndInactiveAccounts()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff(isActive: false);
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.FindByEmailAsync(staff.Email)).ReturnsAsync(staff);
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = LoginPageWithInput(context, userManager, signInManager);
            page.ModelState.AddModelError("Input.Email", "invalid");

            Assert.IsType<PageResult>(await page.OnPostAsync("/Claims"));
            page.ModelState.Clear();
            Assert.IsType<PageResult>(await page.OnPostAsync("/Claims"));
            Assert.Contains(page.ModelState[string.Empty].Errors,
                error => error.ErrorMessage == "Invalid email or password.");
            signInManager.Verify(manager => manager.PasswordSignInAsync(
                It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task LoginRejectsAccountWhoseOrganizationIsInactive()
        {
            await using var context = TestInfrastructure.CreateContext();
            var organization = await context.Organizations.SingleAsync();
            organization.IsActive = false;
            await context.SaveChangesAsync();
            var staff = Staff();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.FindByEmailAsync(staff.Email)).ReturnsAsync(staff);
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = LoginPageWithInput(context, userManager, signInManager);

            Assert.IsType<PageResult>(await page.OnPostAsync());
            Assert.False(page.ModelState.IsValid);
        }

        [Theory]
        [InlineData("success")]
        [InlineData("two-factor")]
        [InlineData("locked")]
        [InlineData("failed")]
        public async Task LoginHandlesEveryIdentitySignInOutcome(string outcome)
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.FindByEmailAsync(staff.Email)).ReturnsAsync(staff);
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var signInResult = outcome switch
            {
                "success" => IdentitySignInResult.Success,
                "two-factor" => IdentitySignInResult.TwoFactorRequired,
                "locked" => IdentitySignInResult.LockedOut,
                _ => IdentitySignInResult.Failed
            };
            signInManager.Setup(manager => manager.PasswordSignInAsync(staff, "Password1!Test", true, true))
                .ReturnsAsync(signInResult);
            var page = LoginPageWithInput(context, userManager, signInManager);

            var result = await page.OnPostAsync("/Claims");

            if (outcome == "success")
            {
                Assert.Equal("/Claims", Assert.IsType<LocalRedirectResult>(result).Url);
            }
            else if (outcome == "two-factor")
            {
                Assert.Equal("/Account/LoginWithTwoFactor", Assert.IsType<RedirectToPageResult>(result).PageName);
            }
            else
            {
                Assert.IsType<PageResult>(result);
                Assert.False(page.ModelState.IsValid);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TwoFactorLoginValidatesPendingAccount(bool activeAccount)
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff(isActive: activeAccount);
            var userManager = TestInfrastructure.CreateUserManager();
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            signInManager.Setup(manager => manager.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(staff);
            var page = new TwoFactorLoginPage(signInManager.Object, context) { ReturnUrl = "/Claims" };
            TestInfrastructure.InitializePage(page);

            var result = await page.OnGetAsync();

            if (activeAccount)
            {
                Assert.IsType<PageResult>(result);
            }
            else
            {
                Assert.Equal("Login", Assert.IsType<RedirectToPageResult>(result).PageName);
            }
        }

        [Theory]
        [InlineData("success")]
        [InlineData("locked")]
        [InlineData("failed")]
        public async Task TwoFactorLoginNormalizesCodeAndHandlesSignInOutcome(string outcome)
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff();
            var userManager = TestInfrastructure.CreateUserManager();
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            signInManager.Setup(manager => manager.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(staff);
            var signInResult = outcome == "success"
                ? IdentitySignInResult.Success
                : outcome == "locked" ? IdentitySignInResult.LockedOut : IdentitySignInResult.Failed;
            signInManager.Setup(manager => manager.TwoFactorAuthenticatorSignInAsync("123456", true, true))
                .ReturnsAsync(signInResult);
            var page = new TwoFactorLoginPage(signInManager.Object, context)
            {
                ReturnUrl = "/Claims",
                RememberMe = true,
                Input = new TwoFactorLoginInputModel { Code = "123 456", RememberMachine = true }
            };
            TestInfrastructure.InitializePage(page);

            var result = await page.OnPostAsync();

            if (outcome == "success")
            {
                Assert.Equal("/Claims", Assert.IsType<LocalRedirectResult>(result).Url);
            }
            else
            {
                Assert.IsType<PageResult>(result);
                Assert.False(page.ModelState.IsValid);
            }
        }

        [Theory]
        [InlineData("success")]
        [InlineData("locked")]
        [InlineData("failed")]
        public async Task RecoveryLoginNormalizesCodeAndHandlesSignInOutcome(string outcome)
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff();
            var userManager = TestInfrastructure.CreateUserManager();
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            signInManager.Setup(manager => manager.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(staff);
            var signInResult = outcome == "success"
                ? IdentitySignInResult.Success
                : outcome == "locked" ? IdentitySignInResult.LockedOut : IdentitySignInResult.Failed;
            signInManager.Setup(manager => manager.TwoFactorRecoveryCodeSignInAsync("codevalue"))
                .ReturnsAsync(signInResult);
            var page = new RecoveryLoginPage(signInManager.Object, context)
            {
                ReturnUrl = "/Claims",
                Input = new RecoveryCodeInputModel { Code = "code value" }
            };
            TestInfrastructure.InitializePage(page);

            var result = await page.OnPostAsync();

            if (outcome == "success")
            {
                Assert.Equal("/Claims", Assert.IsType<LocalRedirectResult>(result).Url);
            }
            else
            {
                Assert.IsType<PageResult>(result);
                Assert.False(page.ModelState.IsValid);
            }
        }

        [Fact]
        public async Task RecoveryLoginRedirectsWhenPendingAccountIsUnavailable()
        {
            await using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            signInManager.Setup(manager => manager.GetTwoFactorAuthenticationUserAsync())
                .ReturnsAsync((ApplicationUser)null);
            var page = new RecoveryLoginPage(signInManager.Object, context)
            {
                ReturnUrl = "https://attacker.example"
            };
            TestInfrastructure.InitializePage(page);

            Assert.Equal("Login", Assert.IsType<RedirectToPageResult>(await page.OnGetAsync()).PageName);
            Assert.Equal("Login", Assert.IsType<RedirectToPageResult>(await page.OnPostAsync()).PageName);
            Assert.Equal("/generated", page.ReturnUrl);
        }

        [Fact]
        public async Task ChangePasswordHandlesMissingUserFailureAndSuccess()
        {
            var userManager = TestInfrastructure.CreateUserManager();
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = new ChangePasswordPage(userManager.Object, signInManager.Object)
            {
                Input = new ChangePasswordInputModel
                {
                    CurrentPassword = "OldPassword1!",
                    NewPassword = "NewPassword1!",
                    ConfirmPassword = "NewPassword1!"
                }
            };
            TestInfrastructure.InitializePage(page);
            userManager.Setup(manager => manager.GetUserAsync(page.User)).ReturnsAsync((ApplicationUser)null);
            Assert.IsType<ChallengeResult>(await page.OnPostAsync());

            var staff = Staff();
            userManager.Setup(manager => manager.GetUserAsync(page.User)).ReturnsAsync(staff);
            userManager.Setup(manager => manager.ChangePasswordAsync(staff, "OldPassword1!", "NewPassword1!"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Wrong password" }));
            Assert.IsType<PageResult>(await page.OnPostAsync());
            Assert.False(page.ModelState.IsValid);

            page.ModelState.Clear();
            userManager.Setup(manager => manager.ChangePasswordAsync(staff, "OldPassword1!", "NewPassword1!"))
                .ReturnsAsync(IdentityResult.Success);
            var result = await page.OnPostAsync();
            Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(result).PageName);
            Assert.Equal("Your password has been changed.", page.TempData["StatusMessage"]);
            signInManager.Verify(manager => manager.RefreshSignInAsync(staff), Times.Once);
        }

        [Fact]
        public async Task LogoutSignsOutAndRedirectsToLogin()
        {
            var userManager = TestInfrastructure.CreateUserManager();
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = new LogoutPage(signInManager.Object);

            var result = await page.OnPostAsync();

            Assert.Equal("/Account/Login", Assert.IsType<RedirectToPageResult>(result).PageName);
            signInManager.Verify(manager => manager.SignOutAsync(), Times.Once);
        }

        [Fact]
        public void ResetPasswordGetRequiresBothTokenValues()
        {
            using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var page = new ResetPasswordPage(context, userManager.Object);

            Assert.IsType<NotFoundResult>(page.OnGet(null, "code"));
            Assert.IsType<NotFoundResult>(page.OnGet("user", null));
            Assert.IsType<PageResult>(page.OnGet("user", "code"));
            Assert.Equal("user", page.Input.UserId);
        }

        [Fact]
        public async Task ResetPasswordRejectsUnknownUserAndInvalidToken()
        {
            await using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var page = ResetPageWithInput(context, userManager);
            userManager.Setup(manager => manager.FindByIdAsync("staff-id")).ReturnsAsync((ApplicationUser)null);
            Assert.IsType<PageResult>(await page.OnPostAsync());
            Assert.False(page.ModelState.IsValid);

            page.ModelState.Clear();
            var staff = Staff();
            userManager.Setup(manager => manager.FindByIdAsync("staff-id")).ReturnsAsync(staff);
            userManager.Setup(manager => manager.ResetPasswordAsync(staff, "token", "NewPassword1!"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidToken",
                    Description = "Original provider message"
                }));
            Assert.IsType<PageResult>(await page.OnPostAsync());
            Assert.Contains(page.ModelState[string.Empty].Errors,
                error => error.ErrorMessage.Contains("invalid or has expired"));
        }

        [Fact]
        public async Task ResetPasswordConfirmsAccountAndCreatesAuditEvent()
        {
            await using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var staff = Staff();
            var page = ResetPageWithInput(context, userManager);
            userManager.Setup(manager => manager.FindByIdAsync("staff-id")).ReturnsAsync(staff);
            userManager.Setup(manager => manager.ResetPasswordAsync(staff, "token", "NewPassword1!"))
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(manager => manager.UpdateAsync(staff)).ReturnsAsync(IdentityResult.Success);

            var result = await page.OnPostAsync();

            Assert.Equal("Login", Assert.IsType<RedirectToPageResult>(result).PageName);
            Assert.True(staff.EmailConfirmed);
            Assert.NotNull(staff.UpdatedUtc);
            var audit = await context.AuditEvents.SingleAsync();
            Assert.Equal(Security.AuditEventTypes.PasswordSet, audit.EventType);
            Assert.Equal(staff.Id, audit.ActorUserId);
            Assert.Equal("Your password has been set. You can now sign in.", page.TempData["StatusMessage"]);
        }

        private static LoginPage LoginPageWithInput(
            global::InsuranceClaimManagement.Data.ApplicationDbContext context,
            Mock<UserManager<ApplicationUser>> userManager,
            Mock<SignInManager<ApplicationUser>> signInManager)
        {
            var page = new LoginPage(signInManager.Object, userManager.Object, context)
            {
                Input = new LoginPage.InputModel
                {
                    Email = " staff@example.test ",
                    Password = "Password1!Test",
                    RememberMe = true
                }
            };
            TestInfrastructure.InitializePage(page);
            return page;
        }

        private static ResetPasswordPage ResetPageWithInput(
            global::InsuranceClaimManagement.Data.ApplicationDbContext context,
            Mock<UserManager<ApplicationUser>> userManager)
        {
            var page = new ResetPasswordPage(context, userManager.Object)
            {
                Input = new ResetPasswordInputModel
                {
                    UserId = "staff-id",
                    Code = "token",
                    Password = "NewPassword1!",
                    ConfirmPassword = "NewPassword1!"
                }
            };
            TestInfrastructure.InitializePage(page);
            return page;
        }

        private static ApplicationUser Staff(bool isActive = true)
        {
            return new ApplicationUser
            {
                Id = "staff-id",
                OrganizationId = TestInfrastructure.OrganizationId,
                DisplayName = "Test Staff",
                UserName = "staff@example.test",
                Email = "staff@example.test",
                IsActive = isActive,
                EmailConfirmed = true
            };
        }
    }
}
