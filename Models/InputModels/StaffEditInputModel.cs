using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimManagement.Models.InputModels
{
    public class StaffEditInputModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; }

        [Required]
        public string Role { get; set; }

        [Display(Name = "Account active")]
        public bool IsActive { get; set; }
    }
}
