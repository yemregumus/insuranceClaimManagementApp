using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimManagement.Models.InputModels
{
    public class RecoveryCodeInputModel
    {
        [Required]
        [Display(Name = "Recovery code")]
        public string Code { get; set; }
    }
}
