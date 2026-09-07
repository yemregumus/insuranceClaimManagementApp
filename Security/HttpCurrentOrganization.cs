using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace InsuranceClaimManagement.Security
{
    public class HttpCurrentOrganization : ICurrentOrganization
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpCurrentOrganization(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? OrganizationId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirst(TenantClaimTypes.OrganizationId)?.Value;
                return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var organizationId)
                    ? organizationId
                    : null;
            }
        }

        public int GetRequiredOrganizationId()
        {
            return OrganizationId ?? throw new InvalidOperationException(
                "The signed-in account is not assigned to an organization. Sign out and contact an administrator.");
        }
    }
}
