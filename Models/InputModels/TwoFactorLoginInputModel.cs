using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimManagement.Models.InputModels
{
    public class TwoFactorLoginInputModel
    {
        [Required]
        [StringLength(8, MinimumLength = 6)]
        [Display(Name = "Authenticator code")]
        public string Code { get; set; }

        [Display(Name = "Remember this browser")]
        public bool RememberMachine { get; set; }
    }
}
