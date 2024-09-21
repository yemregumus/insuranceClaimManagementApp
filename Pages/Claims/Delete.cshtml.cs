using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimManagement.Data;
using InsuranceClaimManagement.Models;

namespace InsuranceClaimManagement.Pages.Claims
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Claim Claim { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Claim = await _context.Claims.FindAsync(id);

            if (Claim == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Claim == null)
            {
                return NotFound();
            }

            _context.Claims.Remove(Claim);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}
