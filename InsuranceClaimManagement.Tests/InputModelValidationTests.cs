using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using InsuranceClaimManagement.Models.InputModels;
using InsuranceClaimManagement.Security;
using Xunit;

namespace InsuranceClaimManagement.Tests
{
    public class InputModelValidationTests
    {
        [Fact]
        public void ClaimInputRejectsNonPositiveAmountAndMissingCustomer()
        {
            var model = new ClaimInputModel
            {
                ClaimType = "Health",
                Description = "Synthetic test claim",
                PolicyNumber = "TEST-001",
                ClaimDate = DateTime.Today,
                Amount = 0,
                Status = "Pending",
                UserId = 0
            };

            var validationResults = Validate(model);

            Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(model.Amount)));
            Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(model.UserId)));
        }

        [Fact]
        public void ClaimInputAcceptsValidValues()
        {
            var model = new ClaimInputModel
            {
                ClaimType = "Health",
                Description = "Synthetic test claim",
                PolicyNumber = "TEST-002",
                ClaimDate = DateTime.Today,
                Amount = 125.50m,
                Status = "Pending",
                UserId = 1
            };

            Assert.Empty(Validate(model));
        }

        [Fact]
        public void CustomerInputRejectsInvalidEmail()
        {
            var model = new CustomerInputModel
            {
                Name = "Test Customer",
                Email = "not-an-email",
                Username = "TEST-CUSTOMER"
            };

            var validationResults = Validate(model);

            Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(model.Email)));
        }

        [Fact]
        public void PasswordSetupRejectsShortAndMismatchedPasswords()
        {
            var model = new ResetPasswordInputModel
            {
                UserId = "staff-123",
                Code = "test-token",
                Password = "Short1!",
                ConfirmPassword = "Different1!"
            };

            var validationResults = Validate(model);

            Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(model.Password)));
            Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(model.ConfirmPassword)));
        }

        [Fact]
        public void ClaimStatusesRejectArbitraryValues()
        {
            Assert.True(ClaimStatuses.IsValid(ClaimStatuses.Pending));
            Assert.True(ClaimStatuses.IsValid(ClaimStatuses.UnderReview));
            Assert.False(ClaimStatuses.IsValid("Paid without review"));
        }

        [Fact]
        public void StaffInputModelsRejectMissingNamesEmailAndRoles()
        {
            var createResults = Validate(new StaffCreateInputModel
            {
                DisplayName = string.Empty,
                Email = "invalid",
                Role = null
            });
            var editResults = Validate(new StaffEditInputModel
            {
                DisplayName = string.Empty,
                Role = null
            });

            Assert.Contains(createResults, result => result.MemberNames.Contains(nameof(StaffCreateInputModel.DisplayName)));
            Assert.Contains(createResults, result => result.MemberNames.Contains(nameof(StaffCreateInputModel.Email)));
            Assert.Contains(createResults, result => result.MemberNames.Contains(nameof(StaffCreateInputModel.Role)));
            Assert.Contains(editResults, result => result.MemberNames.Contains(nameof(StaffEditInputModel.DisplayName)));
            Assert.Contains(editResults, result => result.MemberNames.Contains(nameof(StaffEditInputModel.Role)));
        }

        [Fact]
        public void TwoFactorAndRecoveryInputsRequireCodesOfExpectedShape()
        {
            var authenticatorResults = Validate(new TwoFactorLoginInputModel { Code = "123" });
            var enableResults = Validate(new EnableTwoFactorInputModel { VerificationCode = "" });
            var recoveryResults = Validate(new RecoveryCodeInputModel { Code = null });

            Assert.NotEmpty(authenticatorResults);
            Assert.NotEmpty(enableResults);
            Assert.NotEmpty(recoveryResults);
        }

        [Fact]
        public void ChangePasswordRequiresMatchingTwelveCharacterPassword()
        {
            var results = Validate(new ChangePasswordInputModel
            {
                CurrentPassword = "OldPassword1!",
                NewPassword = "short",
                ConfirmPassword = "different"
            });

            Assert.Contains(results, result => result.MemberNames.Contains(nameof(ChangePasswordInputModel.NewPassword)));
            Assert.Contains(results, result => result.MemberNames.Contains(nameof(ChangePasswordInputModel.ConfirmPassword)));
        }

        private static List<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
            return results;
        }
    }
}
