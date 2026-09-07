namespace InsuranceClaimManagement.Security
{
    public static class AuditEventTypes
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string Archived = "Archived";
        public const string StaffCreated = "StaffCreated";
        public const string StaffUpdated = "StaffUpdated";
        public const string PasswordSetupLinkCreated = "PasswordSetupLinkCreated";
        public const string PasswordSet = "PasswordSet";
        public const string TwoFactorEnabled = "TwoFactorEnabled";
        public const string RecoveryCodesGenerated = "RecoveryCodesGenerated";
        public const string TwoFactorReset = "TwoFactorReset";
    }
}
