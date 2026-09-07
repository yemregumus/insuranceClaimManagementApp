using System;
using System.Collections.Generic;
using System.Linq;

namespace InsuranceClaimManagement.Security
{
    public static class RoleNames
    {
        public const string Administrator = "Administrator";
        public const string ClaimsAdjuster = "ClaimsAdjuster";
        public const string ReadOnly = "ReadOnly";

        public const string ClaimsReaders = Administrator + "," + ClaimsAdjuster + "," + ReadOnly;
        public const string ClaimsManagers = Administrator + "," + ClaimsAdjuster;

        public static IReadOnlyList<string> AssignableRoles { get; } =
            new[] { Administrator, ClaimsAdjuster, ReadOnly };

        public static bool IsAssignable(string roleName)
        {
            return !string.IsNullOrWhiteSpace(roleName) &&
                   AssignableRoles.Contains(roleName, StringComparer.Ordinal);
        }
    }

    public static class PolicyNames
    {
        public const string ClaimsRead = "ClaimsRead";
        public const string ClaimsManage = "ClaimsManage";
        public const string CustomersManage = "CustomersManage";
        public const string StaffManage = "StaffManage";
    }
}
