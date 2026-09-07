using System;
using System.Security.Claims;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InsuranceClaimManagement.Tests
{
    public class TenantIsolationTests
    {
        [Fact]
        public void CurrentOrganizationReadsOrganizationClaim()
        {
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(TenantClaimTypes.OrganizationId, "17")
                }, "Test"))
            };

            var currentOrganization = new HttpCurrentOrganization(
                new HttpContextAccessor { HttpContext = httpContext });

            Assert.Equal(17, currentOrganization.GetRequiredOrganizationId());
        }

        [Fact]
        public void TenantDataQueriesAreFilteredByOrganization()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySql(
                    "Server=localhost;Database=TestOnly;User=test;Password=test",
                    new MySqlServerVersion(new Version(8, 0, 0)))
                .Options;

            using var context = new ApplicationDbContext(options, new TestCurrentOrganization(23));

            var customerSql = context.Customers.ToQueryString();
            var claimSql = context.Claims.ToQueryString();
            var historySql = context.ClaimStatusHistories.ToQueryString();
            var auditSql = context.AuditEvents.ToQueryString();

            Assert.Contains("OrganizationId", customerSql);
            Assert.Contains("23", customerSql);
            Assert.DoesNotContain("Password", customerSql);
            Assert.Contains("OrganizationId", claimSql);
            Assert.Contains("23", claimSql);
            Assert.Contains("IsDeleted", claimSql);
            Assert.Contains("OrganizationId", historySql);
            Assert.Contains("23", historySql);
            Assert.Contains("OrganizationId", auditSql);
            Assert.Contains("23", auditSql);
        }

        [Fact]
        public void AuditServiceRecordsActorAndOrganizationWithoutSensitiveDetails()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseMySql(
                    "Server=localhost;Database=TestOnly;User=test;Password=test",
                    new MySqlServerVersion(new Version(8, 0, 0)))
                .Options;
            var currentOrganization = new TestCurrentOrganization(31);
            using var context = new ApplicationDbContext(options, currentOrganization);
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, "staff-123"),
                    new System.Security.Claims.Claim(TenantClaimTypes.OrganizationId, "31")
                }, "Test"))
            };
            var auditService = new AuditService(
                context,
                currentOrganization,
                new HttpContextAccessor { HttpContext = httpContext });

            auditService.Record(AuditEventTypes.Updated, "Claim", "42");

            var audit = Assert.Single(context.AuditEvents.Local);
            Assert.Equal(31, audit.OrganizationId);
            Assert.Equal("staff-123", audit.ActorUserId);
            Assert.Equal("42", audit.EntityId);
        }

        private sealed class TestCurrentOrganization : ICurrentOrganization
        {
            public TestCurrentOrganization(int organizationId)
            {
                OrganizationId = organizationId;
            }

            public int? OrganizationId { get; }

            public int GetRequiredOrganizationId()
            {
                return OrganizationId.Value;
            }
        }
    }
}
