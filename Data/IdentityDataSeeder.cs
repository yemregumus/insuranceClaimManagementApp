using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InsuranceClaimManagement.Models;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InsuranceClaimManagement.Data
{
    public static class IdentityDataSeeder
    {
        public static async Task SeedAdministratorAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var email = configuration["InitialAdmin:Email"];
            var password = configuration["InitialAdmin:Password"];
            var organizationName = configuration["InitialOrganization:Name"]?.Trim() ?? "Default Organization";
            var organizationSlug = configuration["InitialOrganization:Slug"]?.Trim().ToLowerInvariant() ?? "default";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Set InitialAdmin__Email and InitialAdmin__Password before running with --seed-admin.");
            }

            email = email.Trim();

            if (string.IsNullOrWhiteSpace(organizationName) || organizationName.Length > 200)
            {
                throw new InvalidOperationException("The organization name is required and cannot exceed 200 characters.");
            }

            if (organizationSlug.Length > 100 ||
                !Regex.IsMatch(organizationSlug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            {
                throw new InvalidOperationException(
                    "The organization slug must contain only lowercase letters, numbers, and single hyphens.");
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            var organization = await context.Organizations
                .SingleOrDefaultAsync(item => item.Slug == organizationSlug);

            if (organization == null)
            {
                organization = new Organization
                {
                    Name = organizationName,
                    Slug = organizationSlug,
                    IsActive = true
                };
                context.Organizations.Add(organization);
                await context.SaveChangesAsync();
            }

            foreach (var roleName in new[] { RoleNames.Administrator, RoleNames.ClaimsAdjuster, RoleNames.ReadOnly })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    EnsureSucceeded(roleResult, $"creating the {roleName} role");
                }
            }

            var administrator = await userManager.FindByEmailAsync(email);
            if (administrator != null && administrator.OrganizationId != organization.Id)
            {
                throw new InvalidOperationException(
                    "That staff email is already assigned to a different organization.");
            }

            if (administrator == null)
            {
                administrator = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = email,
                    OrganizationId = organization.Id,
                    IsActive = true
                };

                var createResult = await userManager.CreateAsync(administrator, password);
                if (!createResult.Succeeded)
                {
                    var validationErrors = FormatErrors(createResult);
                    throw new InvalidOperationException(
                        "The email or password did not meet the account requirements." + Environment.NewLine +
                        "Password requirements:" + Environment.NewLine +
                        "- At least 12 characters" + Environment.NewLine +
                        "- At least one uppercase letter" + Environment.NewLine +
                        "- At least one lowercase letter" + Environment.NewLine +
                        "- At least one number" + Environment.NewLine +
                        "- At least one symbol" + Environment.NewLine +
                        "Validation errors:" + Environment.NewLine +
                        validationErrors);
                }
            }
            else if (administrator.OrganizationId != organization.Id ||
                     string.IsNullOrWhiteSpace(administrator.DisplayName) ||
                     !administrator.IsActive ||
                     !administrator.EmailConfirmed)
            {
                administrator.OrganizationId = organization.Id;
                administrator.DisplayName = string.IsNullOrWhiteSpace(administrator.DisplayName)
                    ? email
                    : administrator.DisplayName;
                administrator.IsActive = true;
                administrator.EmailConfirmed = true;
                administrator.UpdatedUtc = DateTime.UtcNow;
                EnsureSucceeded(await userManager.UpdateAsync(administrator), "updating the administrator account");
            }

            if (!await userManager.IsInRoleAsync(administrator, RoleNames.Administrator))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(administrator, RoleNames.Administrator);
                EnsureSucceeded(addToRoleResult, "assigning the administrator role");
            }

            await transaction.CommitAsync();
            Console.WriteLine($"Administrator account provisioned successfully for organization '{organization.Slug}'.");
        }

        private static void EnsureSucceeded(IdentityResult result, string operation)
        {
            if (result.Succeeded)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Identity failed while {operation}:{Environment.NewLine}{FormatErrors(result)}");
        }

        private static string FormatErrors(IdentityResult result)
        {
            return string.Join(
                Environment.NewLine,
                result.Errors.Select(error => "- " + error.Description));
        }
    }
}
