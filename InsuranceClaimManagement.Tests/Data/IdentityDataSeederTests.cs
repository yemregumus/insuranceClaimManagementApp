using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace InsuranceClaimManagement.Tests.Data
{
    public class IdentityDataSeederTests
    {
        [Fact]
        public async Task ProvisioningRequiresAdministratorCredentials()
        {
            using var provider = CreateProvider(new Dictionary<string, string>());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => IdentityDataSeeder.SeedAdministratorAsync(provider));

            Assert.Contains("InitialAdmin__Email", exception.Message);
        }

        [Theory]
        [InlineData("Invalid Slug")]
        [InlineData("double--hyphen")]
        [InlineData("-leading")]
        public async Task ProvisioningRejectsInvalidOrganizationSlug(string slug)
        {
            using var provider = CreateProvider(ValidConfiguration(slug));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => IdentityDataSeeder.SeedAdministratorAsync(provider));

            Assert.Contains("organization slug", exception.Message);
        }

        [Fact]
        public async Task ProvisioningCreatesOrganizationRolesAndHashedAdministrator()
        {
            using var provider = CreateProvider(ValidConfiguration("acme-insurance"));

            await IdentityDataSeeder.SeedAdministratorAsync(provider);

            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var organization = await context.Organizations.SingleAsync();
            var administrator = await userManager.FindByEmailAsync("admin@example.test");
            Assert.Equal("acme-insurance", organization.Slug);
            Assert.True(organization.IsActive);
            Assert.NotNull(administrator);
            Assert.Equal(organization.Id, administrator.OrganizationId);
            Assert.True(administrator.EmailConfirmed);
            Assert.True(administrator.IsActive);
            Assert.NotEqual("ValidPassword1!", administrator.PasswordHash);
            Assert.True(await userManager.IsInRoleAsync(administrator, RoleNames.Administrator));
            Assert.Equal(3, await context.Roles.CountAsync());
        }

        [Fact]
        public async Task ProvisioningIsIdempotentAndRepairsAdministratorState()
        {
            using var provider = CreateProvider(ValidConfiguration("acme-insurance"));
            await IdentityDataSeeder.SeedAdministratorAsync(provider);
            using (var scope = provider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var administrator = await userManager.FindByEmailAsync("admin@example.test");
                administrator.DisplayName = string.Empty;
                administrator.IsActive = false;
                administrator.EmailConfirmed = false;
                await userManager.UpdateAsync(administrator);
            }

            await IdentityDataSeeder.SeedAdministratorAsync(provider);

            using var verificationScope = provider.CreateScope();
            var context = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var manager = verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var repaired = await manager.FindByEmailAsync("admin@example.test");
            Assert.Equal("admin@example.test", repaired.DisplayName);
            Assert.True(repaired.IsActive);
            Assert.True(repaired.EmailConfirmed);
            Assert.NotNull(repaired.UpdatedUtc);
            Assert.Single(await context.Organizations.ToListAsync());
            Assert.Single(await context.Users.ToListAsync());
        }

        [Fact]
        public async Task ProvisioningDoesNotMoveExistingStaffBetweenOrganizations()
        {
            using var provider = CreateProvider(ValidConfiguration("new-company"));
            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Organizations.Add(new Organization
                {
                    Id = 99,
                    Name = "Existing Company",
                    Slug = "existing-company",
                    IsActive = true
                });
                await context.SaveChangesAsync();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var existing = new ApplicationUser
                {
                    Email = "admin@example.test",
                    UserName = "admin@example.test",
                    DisplayName = "Existing Administrator",
                    OrganizationId = 99,
                    IsActive = true,
                    EmailConfirmed = true
                };
                Assert.True((await userManager.CreateAsync(existing, "ValidPassword1!")).Succeeded);
            }

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => IdentityDataSeeder.SeedAdministratorAsync(provider));

            Assert.Contains("different organization", exception.Message);
        }

        [Fact]
        public async Task ClaimsPrincipalFactoryAddsOrganizationClaim()
        {
            using var provider = CreateProvider(ValidConfiguration("acme-insurance"));
            await IdentityDataSeeder.SeedAdministratorAsync(provider);
            using var scope = provider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var administrator = await userManager.FindByEmailAsync("admin@example.test");
            var factory = new ApplicationUserClaimsPrincipalFactory(
                userManager,
                roleManager,
                scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>());

            var principal = await factory.CreateAsync(administrator);

            Assert.Equal(
                administrator.OrganizationId.ToString(),
                principal.FindFirst(TenantClaimTypes.OrganizationId)?.Value);
            Assert.True(principal.IsInRole(RoleNames.Administrator));
        }

        private static ServiceProvider CreateProvider(IDictionary<string, string> values)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
            var databaseName = "SeederTests-" + Guid.NewGuid();
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
            services.AddSingleton<ICurrentOrganization>(new TestCurrentOrganization(null));
            services.AddDbContext<ApplicationDbContext>(options => options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Password.RequiredLength = 12;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            return services.BuildServiceProvider();
        }

        private static Dictionary<string, string> ValidConfiguration(string slug)
        {
            return new Dictionary<string, string>
            {
                ["InitialAdmin:Email"] = "admin@example.test",
                ["InitialAdmin:Password"] = "ValidPassword1!",
                ["InitialOrganization:Name"] = "Acme Insurance",
                ["InitialOrganization:Slug"] = slug
            };
        }
    }
}
