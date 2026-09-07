using System;
using System.ComponentModel.DataAnnotations;

namespace InsuranceClaimManagement.Models.InputModels
{
    public class ClaimInputModel
    {
        [Required]
        [Display(Name = "Claim type")]
        [StringLength(100)]
        public string ClaimType { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Policy number")]
        [StringLength(255)]
        public string PolicyNumber { get; set; }

        [Required]
        [Display(Name = "Claim date")]
        [DataType(DataType.Date)]
        public DateTime? ClaimDate { get; set; }

        [Range(typeof(decimal), "0.01", "9999999999999999.99")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a customer.")]
        [Display(Name = "Customer")]
        public int UserId { get; set; }

        [StringLength(36)]
        public string ConcurrencyToken { get; set; }
    }
}
