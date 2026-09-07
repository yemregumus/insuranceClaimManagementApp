using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimManagement.Models.InputModels
{
    public class ResetPasswordInputModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 12)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }
    }
}
