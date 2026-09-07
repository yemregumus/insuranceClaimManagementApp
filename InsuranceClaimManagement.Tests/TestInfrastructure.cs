using System;
using System.Collections.Generic;
using System.Security.Claims;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace InsuranceClaimManagement.Tests
{
    internal static class TestInfrastructure
    {
        public const int OrganizationId = 11;
        public const string StaffUserId = "staff-11";

        public static ApplicationDbContext CreateContext(int organizationId = OrganizationId)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new ApplicationDbContext(options, new TestCurrentOrganization(organizationId));
            context.Organizations.Add(new Organization
            {
                Id = organizationId,
                Name = "Test Insurance Company",
                Slug = "test-insurance",
                IsActive = true
            });
            context.SaveChanges();
            return context;
        }

        public static void InitializePage(
            PageModel page,
            string staffUserId = StaffUserId,
            int organizationId = OrganizationId)
        {
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, staffUserId),
                    new System.Security.Claims.Claim(TenantClaimTypes.OrganizationId, organizationId.ToString())
                }, "Test"))
            };
            httpContext.Request.Scheme = "https";

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary());
            page.PageContext = new PageContext(actionContext);
            page.Url = new TestUrlHelper(actionContext);
            page.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        }

        public static Mock<UserManager<ApplicationUser>> CreateUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                NullLogger<UserManager<ApplicationUser>>.Instance);
        }

        public static Mock<SignInManager<ApplicationUser>> CreateSignInManager(
            UserManager<ApplicationUser> userManager)
        {
            return new Mock<SignInManager<ApplicationUser>>(
                userManager,
                new HttpContextAccessor(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                Options.Create(new IdentityOptions()),
                NullLogger<SignInManager<ApplicationUser>>.Instance,
                Mock.Of<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>(),
                Mock.Of<IUserConfirmation<ApplicationUser>>());
        }

        public static Mock<RoleManager<IdentityRole>> CreateRoleManager()
        {
            return new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                Array.Empty<IRoleValidator<IdentityRole>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                NullLogger<RoleManager<IdentityRole>>.Instance);
        }
    }

    internal sealed class TestCurrentOrganization : ICurrentOrganization
    {
        public TestCurrentOrganization(int? organizationId)
        {
            OrganizationId = organizationId;
        }

        public int? OrganizationId { get; }

        public int GetRequiredOrganizationId()
        {
            return OrganizationId ?? throw new InvalidOperationException("No test organization was configured.");
        }
    }

    internal sealed class RecordingAuditService : IAuditService
    {
        public List<(string EventType, string EntityType, string EntityId)> Events { get; } = new();

        public void Record(string eventType, string entityType, string entityId)
        {
            Events.Add((eventType, entityType, entityId));
        }
    }

    internal sealed class TestTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> _values = new();

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>(_values);
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _values = new Dictionary<string, object>(values);
        }
    }

    internal sealed class TestUrlHelper : IUrlHelper
    {
        public TestUrlHelper(ActionContext actionContext)
        {
            ActionContext = actionContext;
        }

        public ActionContext ActionContext { get; }
        public string GeneratedUrl { get; set; } = "/generated";

        public string Action(UrlActionContext actionContext) => GeneratedUrl;
        public string Content(string contentPath) => contentPath;
        public bool IsLocalUrl(string url) =>
            !string.IsNullOrWhiteSpace(url) &&
            url[0] == '/' &&
            (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
        public string Link(string routeName, object values) => GeneratedUrl;
        public string RouteUrl(UrlRouteContext routeContext) => GeneratedUrl;
    }
}
