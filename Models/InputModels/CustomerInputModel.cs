using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimManagement.Models.InputModels
{
    public class CustomerInputModel
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Customer reference")]
        [StringLength(255)]
        public string Username { get; set; }
    }
}
