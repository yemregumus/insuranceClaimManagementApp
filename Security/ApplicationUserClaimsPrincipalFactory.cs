using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using InsuranceClaimManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace InsuranceClaimManagement.Security
{
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new System.Security.Claims.Claim(
                TenantClaimTypes.OrganizationId,
                user.OrganizationId.ToString(CultureInfo.InvariantCulture)));
            return identity;
        }
    }
}
