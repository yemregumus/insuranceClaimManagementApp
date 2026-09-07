-- Run this against insuranceclaimdb before applying the tenant migration.
-- Every result should be zero except the three row-count values.

SELECT COUNT(*) AS DuplicateCustomerEmailGroups
FROM (
    SELECT `Email`
    FROM `Users`
    GROUP BY `Email`
    HAVING COUNT(*) > 1
) AS duplicates;

SELECT COUNT(*) AS DuplicateCustomerReferenceGroups
FROM (
    SELECT `Username`
    FROM `Users`
    GROUP BY `Username`
    HAVING COUNT(*) > 1
) AS duplicates;

SELECT COUNT(*) AS OrphanClaimCount
FROM `Claims` AS claims
LEFT JOIN `Users` AS customers ON customers.`Id` = claims.`UserId`
WHERE customers.`Id` IS NULL;

SELECT
    (SELECT COUNT(*) FROM `Users`) AS CustomerCount,
    (SELECT COUNT(*) FROM `Claims`) AS ClaimCount,
    (SELECT COUNT(*) FROM `AspNetUsers`) AS StaffCount;

SELECT COUNT(*) AS MigrationAlreadyApplied
FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20260906172400_AddOrganizationsAndTenantIsolation';
