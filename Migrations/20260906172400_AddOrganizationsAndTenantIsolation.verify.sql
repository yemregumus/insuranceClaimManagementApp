-- Run after 20260906172400_AddOrganizationsAndTenantIsolation.sql.
-- Expected: one organization, no unassigned/mismatched rows, and one migration record.

SELECT `Id`, `Name`, `Slug`, `IsActive`, `CreatedUtc`
FROM `Organizations`;

SELECT
    (SELECT COUNT(*) FROM `Users` WHERE `OrganizationId` IS NULL) AS UnassignedCustomers,
    (SELECT COUNT(*) FROM `Claims` WHERE `OrganizationId` IS NULL) AS UnassignedClaims,
    (SELECT COUNT(*) FROM `AspNetUsers` WHERE `OrganizationId` IS NULL) AS UnassignedStaff;

SELECT COUNT(*) AS CrossOrganizationClaimCount
FROM `Claims` AS claims
INNER JOIN `Users` AS customers ON customers.`Id` = claims.`UserId`
WHERE claims.`OrganizationId` <> customers.`OrganizationId`;

SELECT
    (SELECT COUNT(*) FROM `Users` WHERE `OrganizationId` = 1) AS DefaultOrganizationCustomers,
    (SELECT COUNT(*) FROM `Claims` WHERE `OrganizationId` = 1) AS DefaultOrganizationClaims,
    (SELECT COUNT(*) FROM `AspNetUsers` WHERE `OrganizationId` = 1 AND `IsActive` = 1) AS DefaultOrganizationActiveStaff;

SELECT COUNT(*) AS MigrationApplied
FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20260906172400_AddOrganizationsAndTenantIsolation';
