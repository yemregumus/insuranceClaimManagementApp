using System;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Identity;
using Xunit;
using AuditIndexModel = InsuranceClaimManagement.Pages.Audit.IndexModel;
using ClaimIndexModel = InsuranceClaimManagement.Pages.Claims.IndexModel;
using CustomerIndexModel = InsuranceClaimManagement.Pages.Users.IndexModel;
using DashboardModel = InsuranceClaimManagement.Pages.IndexModel;
using StaffIndexModel = InsuranceClaimManagement.Pages.Staff.IndexModel;

namespace InsuranceClaimManagement.Tests.Pages
{
    public class ReadOnlyPageTests
    {
        [Fact]
        public async Task DashboardCountsOnlyVisibleTenantRecords()
        {
            await using var context = TestInfrastructure.CreateContext();
            context.Customers.AddRange(Customer(1, TestInfrastructure.OrganizationId, "One"), Customer(2, 99, "Other"));
            context.Claims.AddRange(
                Claim(1, TestInfrastructure.OrganizationId, 1, ClaimStatuses.Pending),
                Claim(2, TestInfrastructure.OrganizationId, 1, ClaimStatuses.Approved),
                Claim(3, TestInfrastructure.OrganizationId, 1, ClaimStatuses.Pending, isDeleted: true),
                Claim(4, 99, 2, ClaimStatuses.Pending));
            await context.SaveChangesAsync();
            var page = new DashboardModel(context);

            await page.OnGetAsync();

            Assert.Equal(1, page.CustomerCount);
            Assert.Equal(2, page.ClaimCount);
            Assert.Equal(1, page.PendingClaimCount);
        }

        [Fact]
        public async Task ClaimAndCustomerIndexesAreTenantScopedAndSorted()
        {
            await using var context = TestInfrastructure.CreateContext();
            context.Customers.AddRange(
                Customer(1, TestInfrastructure.OrganizationId, "Zed"),
                Customer(2, TestInfrastructure.OrganizationId, "Alice"),
                Customer(3, 99, "Other"));
            context.Claims.AddRange(
                Claim(1, TestInfrastructure.OrganizationId, 1, ClaimStatuses.Pending, new DateTime(2026, 1, 1)),
                Claim(2, TestInfrastructure.OrganizationId, 2, ClaimStatuses.Approved, new DateTime(2026, 2, 1)),
                Claim(3, 99, 3, ClaimStatuses.Pending, new DateTime(2026, 3, 1)));
            await context.SaveChangesAsync();

            var customerPage = new CustomerIndexModel(context);
            await customerPage.OnGetAsync();
            Assert.Equal(new[] { "Alice", "Zed" }, customerPage.Users.Select(item => item.Name));

            var claimPage = new ClaimIndexModel(context);
            await claimPage.OnGetAsync();
            Assert.Equal(new[] { 2, 1 }, claimPage.Claims.Select(item => item.Id));
        }

        [Fact]
        public async Task AuditIndexReturnsNewestTwoHundredTenantEvents()
        {
            await using var context = TestInfrastructure.CreateContext();
            var actor = Staff(TestInfrastructure.StaffUserId, TestInfrastructure.OrganizationId, "Auditor");
            context.Users.Add(actor);
            for (var index = 0; index < 205; index++)
            {
                context.AuditEvents.Add(new AuditEvent
                {
                    OrganizationId = TestInfrastructure.OrganizationId,
                    ActorUserId = actor.Id,
                    EventType = AuditEventTypes.Updated,
                    EntityType = nameof(Claim),
                    EntityId = index.ToString(),
                    OccurredUtc = new DateTime(2026, 1, 1).AddMinutes(index)
                });
            }
            context.AuditEvents.Add(new AuditEvent
            {
                OrganizationId = 99,
                ActorUserId = "other-staff",
                EventType = AuditEventTypes.Updated,
                EntityType = nameof(Claim),
                EntityId = "other",
                OccurredUtc = new DateTime(2027, 1, 1)
            });
            await context.SaveChangesAsync();
            var page = new AuditIndexModel(context);

            await page.OnGetAsync();

            Assert.Equal(200, page.AuditEvents.Count);
            Assert.Equal("204", page.AuditEvents[0].EntityId);
            Assert.Equal("5", page.AuditEvents[^1].EntityId);
        }

        [Fact]
        public async Task StaffIndexShowsCurrentTenantRolesAndNoRoleFallback()
        {
            await using var context = TestInfrastructure.CreateContext();
            var administrator = Staff("admin", TestInfrastructure.OrganizationId, "Alice");
            var noRole = Staff("no-role", TestInfrastructure.OrganizationId, "Bob");
            var otherTenant = Staff("other", 99, "Other");
            var role = new IdentityRole(RoleNames.Administrator) { Id = "role-admin" };
            context.AddRange(administrator, noRole, otherTenant, role);
            context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = administrator.Id,
                RoleId = role.Id
            });
            await context.SaveChangesAsync();
            var page = new StaffIndexModel(
                context,
                new TestCurrentOrganization(TestInfrastructure.OrganizationId));

            await page.OnGetAsync();

            Assert.Equal(2, page.StaffMembers.Count);
            Assert.Equal(RoleNames.Administrator, page.StaffMembers[0].Roles);
            Assert.Equal("No role", page.StaffMembers[1].Roles);
        }

        private static User Customer(int id, int organizationId, string name)
        {
            return new User
            {
                Id = id,
                OrganizationId = organizationId,
                Name = name,
                Email = $"customer{id}-{organizationId}@example.test",
                Username = $"REF-{organizationId}-{id}"
            };
        }

        private static Claim Claim(
            int id,
            int organizationId,
            int userId,
            string status,
            DateTime? date = null,
            bool isDeleted = false)
        {
            return new Claim
            {
                Id = id,
                OrganizationId = organizationId,
                UserId = userId,
                ClaimType = "Health",
                Description = "Test",
                PolicyNumber = $"POL-{id}",
                Amount = 10,
                Status = status,
                ClaimDate = date,
                IsDeleted = isDeleted
            };
        }

        private static ApplicationUser Staff(string id, int organizationId, string displayName)
        {
            return new ApplicationUser
            {
                Id = id,
                OrganizationId = organizationId,
                DisplayName = displayName,
                UserName = $"{id}@example.test",
                Email = $"{id}@example.test",
                IsActive = true
            };
        }
    }
}
