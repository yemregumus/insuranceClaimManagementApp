using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsuranceClaimManagement.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaimManagement.Pages.Audit
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Models.AuditEvent> AuditEvents { get; private set; }

        public async Task OnGetAsync()
        {
            AuditEvents = await _context.AuditEvents
                .AsNoTracking()
                .Include(audit => audit.ActorUser)
                .OrderByDescending(audit => audit.OccurredUtc)
                .Take(200)
                .ToListAsync();
        }
    }
}
