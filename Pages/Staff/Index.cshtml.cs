using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Security;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimManagement.Pages.Staff
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentOrganization _currentOrganization;

        public IndexModel(ApplicationDbContext context, ICurrentOrganization currentOrganization)
        {
            _context = context;
            _currentOrganization = currentOrganization;
        }

        public List<StaffListItem> StaffMembers { get; private set; } = new List<StaffListItem>();

        public async Task OnGetAsync()
        {
            var organizationId = _currentOrganization.GetRequiredOrganizationId();
            var staff = await _context.Users
                .AsNoTracking()
                .Where(item => item.OrganizationId == organizationId)
                .OrderBy(item => item.DisplayName)
                .Select(item => new
                {
                    item.Id,
                    item.DisplayName,
                    item.Email,
                    item.IsActive
                })
                .ToListAsync();

            var staffIds = staff.Select(item => item.Id).ToList();
            var roleAssignments = await (
                from assignment in _context.UserRoles.AsNoTracking()
                join role in _context.Roles.AsNoTracking() on assignment.RoleId equals role.Id
                where staffIds.Contains(assignment.UserId)
                select new { assignment.UserId, role.Name })
                .ToListAsync();

            var rolesByStaffId = roleAssignments
                .GroupBy(item => item.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => string.Join(", ", group.Select(item => item.Name).OrderBy(name => name)));

            StaffMembers = staff.Select(item => new StaffListItem
            {
                Id = item.Id,
                DisplayName = item.DisplayName,
                Email = item.Email,
                IsActive = item.IsActive,
                Roles = rolesByStaffId.TryGetValue(item.Id, out var roles) ? roles : "No role"
            }).ToList();
        }

        public class StaffListItem
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string Email { get; set; }
            public bool IsActive { get; set; }
            public string Roles { get; set; }
        }
    }
}
