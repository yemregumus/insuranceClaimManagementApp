using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimManagement.Models.InputModels
{
    public class StaffCreateInputModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; }

        [Required]
        public string Role { get; set; }

    }
}
