using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using InsuranceClaimManagement.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InsuranceClaimManagement.Tests
{
    public class AuthorizationPolicyTests
    {
        [Fact]
        public async Task ClaimsManagePolicyAllowsOnlyAdministratorsAndAdjusters()
        {
            using var provider = CreateServiceProvider();
            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            var policy = await policyProvider.GetPolicyAsync(PolicyNames.ClaimsManage);
            var roleRequirement = Assert.Single(policy.Requirements.OfType<RolesAuthorizationRequirement>());

            Assert.Equal(
                new[] { RoleNames.Administrator, RoleNames.ClaimsAdjuster }.OrderBy(role => role),
                roleRequirement.AllowedRoles.OrderBy(role => role));
        }

        [Fact]
        public async Task CustomersManagePolicyAllowsOnlyAdministrators()
        {
            using var provider = CreateServiceProvider();
            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            var policy = await policyProvider.GetPolicyAsync(PolicyNames.CustomersManage);
            var roleRequirement = Assert.Single(policy.Requirements.OfType<RolesAuthorizationRequirement>());

            Assert.Equal(new[] { RoleNames.Administrator }, roleRequirement.AllowedRoles);
        }

        [Fact]
        public async Task StaffManagePolicyRequiresAdministratorAndOrganization()
        {
            using var provider = CreateServiceProvider();
            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            var policy = await policyProvider.GetPolicyAsync(PolicyNames.StaffManage);
            var roleRequirement = Assert.Single(policy.Requirements.OfType<RolesAuthorizationRequirement>());
            var organizationRequirement = Assert.Single(
                policy.Requirements.OfType<ClaimsAuthorizationRequirement>(),
                requirement => requirement.ClaimType == TenantClaimTypes.OrganizationId);

            Assert.Equal(new[] { RoleNames.Administrator }, roleRequirement.AllowedRoles);
            Assert.Null(organizationRequirement.AllowedValues);
        }

        [Fact]
        public async Task ConfiguredPasswordValidatorRejectsPasswordMissingRequiredCharacterTypes()
        {
            using var provider = CreateServiceProvider();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

            var result = await userManager.PasswordValidators[0].ValidateAsync(
                userManager,
                new ApplicationUser(),
                "alllowercasepassword");

            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, error => error.Code == "PasswordRequiresDigit");
            Assert.Contains(result.Errors, error => error.Code == "PasswordRequiresUpper");
            Assert.Contains(result.Errors, error => error.Code == "PasswordRequiresNonAlphanumeric");
        }

        [Fact]
        public void SignInRequiresConfirmedEmailAndSetupTokensExpireQuickly()
        {
            using var provider = CreateServiceProvider();

            var identityOptions = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;
            var tokenOptions = provider.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value;

            Assert.True(identityOptions.SignIn.RequireConfirmedEmail);
            Assert.Equal(System.TimeSpan.FromHours(2), tokenOptions.TokenLifespan);
        }

        private static ServiceProvider CreateServiceProvider()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=localhost;Port=3306;Database=TestOnly;User=test;Password=test"
                })
                .Build();

            var services = new ServiceCollection();
            new Startup(configuration).ConfigureServices(services);
            return services.BuildServiceProvider();
        }
    }
}
