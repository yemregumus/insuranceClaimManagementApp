using System;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Models.InputModels;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ClaimCreateModel = InsuranceClaimManagement.Pages.Claims.CreateModel;
using ClaimDeleteModel = InsuranceClaimManagement.Pages.Claims.DeleteModel;
using ClaimEditModel = InsuranceClaimManagement.Pages.Claims.EditModel;

namespace InsuranceClaimManagement.Tests.Claims
{
    public class ClaimManagementPageTests
    {
        [Fact]
        public async Task CreateGetInitializesDefaultsAndTenantCustomerOptions()
        {
            await using var context = TestInfrastructure.CreateContext();
            context.Customers.AddRange(
                Customer(1, TestInfrastructure.OrganizationId, "Zed"),
                Customer(2, TestInfrastructure.OrganizationId, "Alice"),
                Customer(3, 99, "Other tenant"));
            await context.SaveChangesAsync();
            var page = new ClaimCreateModel(
                context,
                new TestCurrentOrganization(TestInfrastructure.OrganizationId),
                new RecordingAuditService());

            await page.OnGetAsync();

            Assert.Equal(ClaimStatuses.Pending, page.Input.Status);
            Assert.Equal(DateTime.Today, page.Input.ClaimDate);
            Assert.Equal(new[] { "Alice (REF-2)", "Zed (REF-1)" }, page.CustomerOptions.Select(item => item.Text));
            Assert.Equal(ClaimStatuses.All, page.StatusOptions.Select(item => item.Value));
        }

        [Fact]
        public async Task CreatePostRejectsMissingInputAndCrossTenantCustomer()
        {
            await using var context = TestInfrastructure.CreateContext();
            context.Customers.Add(Customer(7, 99, "Other tenant"));
            await context.SaveChangesAsync();
            var page = new ClaimCreateModel(
                context,
                new TestCurrentOrganization(TestInfrastructure.OrganizationId),
                new RecordingAuditService());
            TestInfrastructure.InitializePage(page);

            var missingResult = await page.OnPostAsync();
            Assert.IsType<PageResult>(missingResult);
            Assert.False(page.ModelState.IsValid);

            page.ModelState.Clear();
            page.Input = ValidInput(userId: 7);
            var crossTenantResult = await page.OnPostAsync();

            Assert.IsType<PageResult>(crossTenantResult);
            Assert.Contains(page.ModelState["Input.UserId"].Errors,
                error => error.ErrorMessage == "Select an existing customer.");
        }

        [Fact]
        public async Task CreatePostPersistsClaimHistoryAndAuditForCurrentTenant()
        {
            await using var context = TestInfrastructure.CreateContext();
            context.Customers.Add(Customer(1, TestInfrastructure.OrganizationId, "Customer"));
            await context.SaveChangesAsync();
            var audit = new RecordingAuditService();
            var page = new ClaimCreateModel(
                context,
                new TestCurrentOrganization(TestInfrastructure.OrganizationId),
                audit)
            {
                Input = ValidInput(1)
            };
            TestInfrastructure.InitializePage(page);

            var result = await page.OnPostAsync();

            var redirect = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("Index", redirect.PageName);
            var claim = await context.Claims.Include(item => item.StatusHistory).SingleAsync();
            Assert.Equal(TestInfrastructure.OrganizationId, claim.OrganizationId);
            Assert.Equal("Health", claim.ClaimType);
            Assert.Equal("Policy notes", claim.Description);
            Assert.Equal("POL-100", claim.PolicyNumber);
            var history = Assert.Single(claim.StatusHistory);
            Assert.Null(history.PreviousStatus);
            Assert.Equal(ClaimStatuses.Pending, history.NewStatus);
            Assert.Equal(TestInfrastructure.StaffUserId, history.ChangedByUserId);
            Assert.Contains(audit.Events, item =>
                item.EventType == AuditEventTypes.Created && item.EntityId == claim.Id.ToString());
        }

        [Fact]
        public async Task EditGetReturnsNotFoundOrLoadsClaimAndHistory()
        {
            await using var context = TestInfrastructure.CreateContext();
            var customer = Customer(1, TestInfrastructure.OrganizationId, "Customer");
            var claim = ClaimRecord(10, customer);
            context.AddRange(customer, claim, new ApplicationUser
            {
                Id = TestInfrastructure.StaffUserId,
                OrganizationId = TestInfrastructure.OrganizationId,
                DisplayName = "Test Staff",
                Email = "staff@example.test",
                UserName = "staff@example.test"
            });
            context.ClaimStatusHistories.Add(new ClaimStatusHistory
            {
                OrganizationId = TestInfrastructure.OrganizationId,
                ClaimId = claim.Id,
                PreviousStatus = null,
                NewStatus = ClaimStatuses.Pending,
                ChangedUtc = DateTime.UtcNow,
                ChangedByUserId = TestInfrastructure.StaffUserId
            });
            await context.SaveChangesAsync();
            var page = new ClaimEditModel(context, new RecordingAuditService());

            Assert.IsType<NotFoundResult>(await page.OnGetAsync(999));
            Assert.IsType<PageResult>(await page.OnGetAsync(claim.Id));
            Assert.Equal(claim.ConcurrencyToken, page.Input.ConcurrencyToken);
            Assert.Single(page.StatusHistory);
        }

        [Fact]
        public async Task EditPostRejectsInvalidStatusAndMissingVersion()
        {
            await using var context = TestInfrastructure.CreateContext();
            var customer = Customer(1, TestInfrastructure.OrganizationId, "Customer");
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
            var page = new ClaimEditModel(context, new RecordingAuditService())
            {
                Input = ValidInput(customer.Id)
            };
            page.Input.Status = "Arbitrary";
            page.Input.ConcurrencyToken = null;
            TestInfrastructure.InitializePage(page);

            var result = await page.OnPostAsync(1);

            Assert.IsType<PageResult>(result);
            Assert.Contains(page.ModelState["Input.Status"].Errors,
                error => error.ErrorMessage == "Select a valid claim status.");
            Assert.Contains(page.ModelState[string.Empty].Errors,
                error => error.ErrorMessage.Contains("version is missing"));
        }

        [Fact]
        public async Task EditPostUpdatesClaimAndAddsStatusHistory()
        {
            await using var context = TestInfrastructure.CreateContext();
            var customer = Customer(1, TestInfrastructure.OrganizationId, "Customer");
            var claim = ClaimRecord(10, customer);
            context.AddRange(customer, claim);
            await context.SaveChangesAsync();
            var originalToken = claim.ConcurrencyToken;
            context.ChangeTracker.Clear();
            var audit = new RecordingAuditService();
            var page = new ClaimEditModel(context, audit)
            {
                Input = ValidInput(customer.Id)
            };
            page.Input.Status = ClaimStatuses.Approved;
            page.Input.ConcurrencyToken = originalToken;
            TestInfrastructure.InitializePage(page);

            var result = await page.OnPostAsync(claim.Id);

            Assert.IsType<RedirectToPageResult>(result);
            var updated = await context.Claims.SingleAsync(item => item.Id == claim.Id);
            Assert.Equal(ClaimStatuses.Approved, updated.Status);
            Assert.NotEqual(originalToken, updated.ConcurrencyToken);
            var history = await context.ClaimStatusHistories.SingleAsync();
            Assert.Equal(ClaimStatuses.Pending, history.PreviousStatus);
            Assert.Equal(ClaimStatuses.Approved, history.NewStatus);
            Assert.Contains(audit.Events, item => item.EventType == AuditEventTypes.Updated);
        }

        [Fact]
        public async Task DeleteGetAndPostArchiveInsteadOfRemovingClaim()
        {
            await using var context = TestInfrastructure.CreateContext();
            var customer = Customer(1, TestInfrastructure.OrganizationId, "Customer");
            var claim = ClaimRecord(10, customer);
            context.AddRange(customer, claim);
            await context.SaveChangesAsync();
            var audit = new RecordingAuditService();
            var page = new ClaimDeleteModel(context, audit);
            TestInfrastructure.InitializePage(page);

            Assert.IsType<NotFoundResult>(await page.OnGetAsync(999));
            Assert.IsType<PageResult>(await page.OnGetAsync(claim.Id));
            Assert.Equal(claim.ConcurrencyToken, page.ConcurrencyToken);

            page.ConcurrencyToken = claim.ConcurrencyToken;
            var result = await page.OnPostAsync(claim.Id);

            Assert.IsType<RedirectToPageResult>(result);
            Assert.Empty(await context.Claims.ToListAsync());
            var archived = await context.Claims.IgnoreQueryFilters().SingleAsync();
            Assert.True(archived.IsDeleted);
            Assert.NotNull(archived.DeletedUtc);
            Assert.Equal(TestInfrastructure.StaffUserId, archived.DeletedByUserId);
            Assert.Contains(audit.Events, item => item.EventType == AuditEventTypes.Archived);
        }

        [Fact]
        public async Task DeletePostRejectsMissingClaimAndMissingVersion()
        {
            await using var context = TestInfrastructure.CreateContext();
            var page = new ClaimDeleteModel(context, new RecordingAuditService());
            TestInfrastructure.InitializePage(page);

            Assert.IsType<NotFoundResult>(await page.OnPostAsync(999));

            var customer = Customer(1, TestInfrastructure.OrganizationId, "Customer");
            var claim = ClaimRecord(10, customer);
            context.AddRange(customer, claim);
            await context.SaveChangesAsync();
            Assert.IsType<BadRequestResult>(await page.OnPostAsync(claim.Id));
        }

        private static ClaimInputModel ValidInput(int userId)
        {
            return new ClaimInputModel
            {
                ClaimType = " Health ",
                Description = " Policy notes ",
                PolicyNumber = " POL-100 ",
                ClaimDate = new DateTime(2026, 1, 2),
                Amount = 125.50m,
                Status = ClaimStatuses.Pending,
                UserId = userId
            };
        }

        private static User Customer(int id, int organizationId, string name)
        {
            return new User
            {
                Id = id,
                OrganizationId = organizationId,
                Name = name,
                Email = $"customer{id}@example.test",
                Username = $"REF-{id}"
            };
        }

        private static Claim ClaimRecord(int id, User customer)
        {
            return new Claim
            {
                Id = id,
                OrganizationId = customer.OrganizationId,
                UserId = customer.Id,
                User = customer,
                ClaimType = "Health",
                Description = "Original",
                PolicyNumber = "POL-OLD",
                ClaimDate = new DateTime(2026, 1, 1),
                Amount = 100,
                Status = ClaimStatuses.Pending
            };
        }
    }
}
