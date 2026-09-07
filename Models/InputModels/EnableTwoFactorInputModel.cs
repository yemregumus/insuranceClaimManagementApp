using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimManagement.Models.InputModels
{
    public class EnableTwoFactorInputModel
    {
        [Required]
        [StringLength(8, MinimumLength = 6)]
        [Display(Name = "Verification code")]
        public string VerificationCode { get; set; }
    }
}
