using System;
using System.Linq;
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
using StaffCreatePage = InsuranceClaimManagement.Pages.Staff.CreateModel;
using StaffEditPage = InsuranceClaimManagement.Pages.Staff.EditModel;

namespace InsuranceClaimManagement.Tests.Staff
{
    public class StaffManagementPageTests
    {
        [Fact]
        public void CreateGetLoadsEveryAssignableRole()
        {
            using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var roleManager = TestInfrastructure.CreateRoleManager();
            var page = CreatePage(context, userManager, roleManager, new RecordingAuditService());

            page.OnGet();

            Assert.Equal(RoleNames.AssignableRoles, page.RoleOptions.Select(item => item.Value));
        }

        [Fact]
        public async Task CreatePostRejectsMissingDetailsAndUnknownRole()
        {
            await using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var roleManager = TestInfrastructure.CreateRoleManager();
            var page = CreatePage(context, userManager, roleManager, new RecordingAuditService());

            Assert.IsType<PageResult>(await page.OnPostAsync());
            Assert.False(page.ModelState.IsValid);

            page.ModelState.Clear();
            page.Input = ValidCreateInput();
            page.Input.Role = "Owner";
            Assert.IsType<PageResult>(await page.OnPostAsync());
            Assert.NotEmpty(page.ModelState["Input.Role"].Errors);
        }

        [Fact]
        public async Task CreatePostReportsIdentityFailureWithoutCreatingSetupLink()
        {
            await using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var roleManager = TestInfrastructure.CreateRoleManager();
            roleManager.Setup(manager => manager.RoleExistsAsync(RoleNames.ClaimsAdjuster)).ReturnsAsync(true);
            userManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate email" }));
            var page = CreatePage(context, userManager, roleManager, new RecordingAuditService());
            page.Input = ValidCreateInput();

            Assert.IsType<PageResult>(await page.OnPostAsync());
            Assert.False(page.AccountCreated);
            Assert.Contains(page.ModelState[string.Empty].Errors,
                error => error.ErrorMessage == "Duplicate email");
        }

        [Fact]
        public async Task CreatePostCreatesTenantStaffRoleAuditAndSetupLink()
        {
            await using var context = TestInfrastructure.CreateContext();
            var userManager = TestInfrastructure.CreateUserManager();
            var roleManager = TestInfrastructure.CreateRoleManager();
            roleManager.Setup(manager => manager.RoleExistsAsync(RoleNames.ClaimsAdjuster)).ReturnsAsync(true);
            userManager.Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .Callback<ApplicationUser, string>((staff, _) => staff.Id = "new-staff")
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), RoleNames.ClaimsAdjuster))
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(manager => manager.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("setup-token");
            var audit = new RecordingAuditService();
            var page = CreatePage(context, userManager, roleManager, audit);
            page.Input = ValidCreateInput();

            var result = await page.OnPostAsync();

            Assert.IsType<PageResult>(result);
            Assert.True(page.AccountCreated);
            Assert.Equal("/generated", page.PasswordSetupUrl);
            userManager.Verify(manager => manager.CreateAsync(
                It.Is<ApplicationUser>(staff =>
                    staff.OrganizationId == TestInfrastructure.OrganizationId &&
                    staff.Email == "adjuster@example.test" &&
                    staff.DisplayName == "Claims Adjuster" &&
                    !staff.EmailConfirmed && staff.IsActive),
                It.Is<string>(password => password.Length >= 12)), Times.Once);
            userManager.Verify(manager => manager.AddToRoleAsync(
                It.IsAny<ApplicationUser>(), RoleNames.ClaimsAdjuster), Times.Once);
            Assert.Contains(audit.Events, item =>
                item.EventType == AuditEventTypes.StaffCreated && item.EntityId == "new-staff");
        }

        [Fact]
        public async Task EditGetIsTenantScopedAndLoadsCurrentRole()
        {
            await using var context = TestInfrastructure.CreateContext();
            var currentStaff = Staff("current", TestInfrastructure.OrganizationId);
            var otherTenant = Staff("other", 99);
            context.Users.AddRange(currentStaff, otherTenant);
            await context.SaveChangesAsync();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.GetRolesAsync(currentStaff))
                .ReturnsAsync(new[] { RoleNames.ClaimsAdjuster });
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = EditPage(context, userManager, signInManager, new RecordingAuditService());

            Assert.IsType<NotFoundResult>(await page.OnGetAsync(otherTenant.Id));
            Assert.IsType<PageResult>(await page.OnGetAsync(currentStaff.Id));
            Assert.Equal(RoleNames.ClaimsAdjuster, page.Input.Role);
            Assert.Equal(currentStaff.Email, page.Email);
            Assert.Equal(RoleNames.AssignableRoles, page.RoleOptions.Select(item => item.Value));
        }

        [Fact]
        public async Task EditPostPreventsAdministratorFromLockingOutSelf()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff(TestInfrastructure.StaffUserId, TestInfrastructure.OrganizationId);
            context.Users.Add(staff);
            await context.SaveChangesAsync();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns(staff.Id);
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = EditPage(context, userManager, signInManager, new RecordingAuditService());
            page.Input = new StaffEditInputModel
            {
                DisplayName = "Self",
                IsActive = false,
                Role = RoleNames.ClaimsAdjuster
            };

            Assert.IsType<PageResult>(await page.OnPostAsync(staff.Id));
            Assert.Contains(page.ModelState[string.Empty].Errors,
                error => error.ErrorMessage.Contains("cannot deactivate your own account"));
        }

        [Fact]
        public async Task EditPostUpdatesStaffRoleAndSecurityStamp()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff("target", TestInfrastructure.OrganizationId);
            context.Users.Add(staff);
            await context.SaveChangesAsync();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns(TestInfrastructure.StaffUserId);
            userManager.Setup(manager => manager.UpdateAsync(staff)).ReturnsAsync(IdentityResult.Success);
            userManager.Setup(manager => manager.GetRolesAsync(staff))
                .ReturnsAsync(new[] { RoleNames.ReadOnly });
            userManager.Setup(manager => manager.RemoveFromRolesAsync(
                    staff, It.IsAny<System.Collections.Generic.IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(manager => manager.AddToRoleAsync(staff, RoleNames.ClaimsAdjuster))
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(manager => manager.UpdateSecurityStampAsync(staff)).ReturnsAsync(IdentityResult.Success);
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var audit = new RecordingAuditService();
            var page = EditPage(context, userManager, signInManager, audit);
            page.Input = new StaffEditInputModel
            {
                DisplayName = " Updated Adjuster ",
                IsActive = true,
                Role = RoleNames.ClaimsAdjuster
            };

            var result = await page.OnPostAsync(staff.Id);

            Assert.Equal("Index", Assert.IsType<RedirectToPageResult>(result).PageName);
            Assert.Equal("Updated Adjuster", staff.DisplayName);
            Assert.NotNull(staff.UpdatedUtc);
            Assert.Equal("Staff account updated.", page.TempData["StatusMessage"]);
            userManager.Verify(manager => manager.RemoveFromRolesAsync(
                staff, It.Is<System.Collections.Generic.IEnumerable<string>>(
                    roles => roles.Single() == RoleNames.ReadOnly)), Times.Once);
            userManager.Verify(manager => manager.AddToRoleAsync(staff, RoleNames.ClaimsAdjuster), Times.Once);
            userManager.Verify(manager => manager.UpdateSecurityStampAsync(staff), Times.Once);
            Assert.Contains(audit.Events, item => item.EventType == AuditEventTypes.StaffUpdated);
        }

        [Fact]
        public async Task PasswordSetupLinkIsTenantScopedAndAudited()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff("target", TestInfrastructure.OrganizationId);
            context.Users.Add(staff);
            await context.SaveChangesAsync();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.GeneratePasswordResetTokenAsync(staff)).ReturnsAsync("token");
            userManager.Setup(manager => manager.GetRolesAsync(staff))
                .ReturnsAsync(new[] { RoleNames.ReadOnly });
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var audit = new RecordingAuditService();
            var page = EditPage(context, userManager, signInManager, audit);

            Assert.IsType<NotFoundResult>(await page.OnPostPasswordSetupAsync("missing"));
            Assert.IsType<PageResult>(await page.OnPostPasswordSetupAsync(staff.Id));
            Assert.Equal("/generated", page.PasswordSetupUrl);
            Assert.Equal(RoleNames.ReadOnly, page.Input.Role);
            Assert.Contains(audit.Events,
                item => item.EventType == AuditEventTypes.PasswordSetupLinkCreated);
        }

        [Fact]
        public async Task ResetTwoFactorDisablesAndRotatesAuthenticatorKey()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff("target", TestInfrastructure.OrganizationId);
            context.Users.Add(staff);
            await context.SaveChangesAsync();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.SetTwoFactorEnabledAsync(staff, false))
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(manager => manager.ResetAuthenticatorKeyAsync(staff))
                .ReturnsAsync(IdentityResult.Success);
            userManager.Setup(manager => manager.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns(staff.Id);
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var audit = new RecordingAuditService();
            var page = EditPage(context, userManager, signInManager, audit);

            Assert.IsType<NotFoundResult>(await page.OnPostResetTwoFactorAsync("missing"));
            var result = await page.OnPostResetTwoFactorAsync(staff.Id);

            Assert.Equal("Index", Assert.IsType<RedirectToPageResult>(result).PageName);
            Assert.Equal("Two-factor authentication was reset.", page.TempData["StatusMessage"]);
            signInManager.Verify(manager => manager.RefreshSignInAsync(staff), Times.Once);
            Assert.Contains(audit.Events, item => item.EventType == AuditEventTypes.TwoFactorReset);
        }

        [Fact]
        public async Task ResetTwoFactorDisplaysIdentityErrors()
        {
            await using var context = TestInfrastructure.CreateContext();
            var staff = Staff("target", TestInfrastructure.OrganizationId);
            context.Users.Add(staff);
            await context.SaveChangesAsync();
            var userManager = TestInfrastructure.CreateUserManager();
            userManager.Setup(manager => manager.SetTwoFactorEnabledAsync(staff, false))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Cannot disable" }));
            userManager.Setup(manager => manager.GetRolesAsync(staff))
                .ReturnsAsync(new[] { RoleNames.ReadOnly });
            var signInManager = TestInfrastructure.CreateSignInManager(userManager.Object);
            var page = EditPage(context, userManager, signInManager, new RecordingAuditService());

            Assert.IsType<PageResult>(await page.OnPostResetTwoFactorAsync(staff.Id));
            Assert.Contains(page.ModelState[string.Empty].Errors,
                error => error.ErrorMessage == "Cannot disable");
            Assert.Equal(RoleNames.ReadOnly, page.Input.Role);
        }

        private static StaffCreatePage CreatePage(
            global::InsuranceClaimManagement.Data.ApplicationDbContext context,
            Mock<UserManager<ApplicationUser>> userManager,
            Mock<RoleManager<IdentityRole>> roleManager,
            IAuditService auditService)
        {
            var page = new StaffCreatePage(
                context,
                userManager.Object,
                roleManager.Object,
                new TestCurrentOrganization(TestInfrastructure.OrganizationId),
                auditService);
            TestInfrastructure.InitializePage(page);
            return page;
        }

        private static StaffEditPage EditPage(
            global::InsuranceClaimManagement.Data.ApplicationDbContext context,
            Mock<UserManager<ApplicationUser>> userManager,
            Mock<SignInManager<ApplicationUser>> signInManager,
            IAuditService auditService)
        {
            var page = new StaffEditPage(
                context,
                userManager.Object,
                signInManager.Object,
                new TestCurrentOrganization(TestInfrastructure.OrganizationId),
                auditService);
            TestInfrastructure.InitializePage(page);
            return page;
        }

        private static StaffCreateInputModel ValidCreateInput()
        {
            return new StaffCreateInputModel
            {
                DisplayName = " Claims Adjuster ",
                Email = " adjuster@example.test ",
                Role = RoleNames.ClaimsAdjuster
            };
        }

        private static ApplicationUser Staff(string id, int organizationId)
        {
            return new ApplicationUser
            {
                Id = id,
                OrganizationId = organizationId,
                DisplayName = $"Staff {id}",
                Email = $"{id}@example.test",
                UserName = $"{id}@example.test",
                IsActive = true
            };
        }
    }
}
