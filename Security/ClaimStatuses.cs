using System;
using System.Collections.Generic;
using System.Linq;

namespace InsuranceClaimManagement.Security
{
    public static class ClaimStatuses
    {
        public const string Pending = "Pending";
        public const string UnderReview = "Under Review";
        public const string Approved = "Approved";
        public const string Denied = "Denied";
        public const string Closed = "Closed";

        public static IReadOnlyList<string> All { get; } =
            new[] { Pending, UnderReview, Approved, Denied, Closed };

        public static bool IsValid(string status)
        {
            return !string.IsNullOrWhiteSpace(status) &&
                   All.Contains(status, StringComparer.Ordinal);
        }
    }
}
