-- Run against insuranceclaimdb before applying the claim history/audit migration.

SELECT COUNT(*) AS PreviousMigrationApplied
FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20260906172400_AddOrganizationsAndTenantIsolation';

SELECT COUNT(*) AS NewMigrationAlreadyApplied
FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20260906180327_AddClaimHistoryAuditAndConcurrency';

SELECT COUNT(*) AS UnsupportedClaimStatusCount
FROM `Claims`
WHERE `Status` NOT IN ('Pending', 'Under Review', 'Approved', 'Denied', 'Closed');

SELECT COUNT(*) AS NewTablesAlreadyPresent
FROM `information_schema`.`TABLES`
WHERE `TABLE_SCHEMA` = DATABASE()
  AND `TABLE_NAME` IN ('AuditEvents', 'ClaimStatusHistory');

SELECT COUNT(*) AS NewClaimColumnsAlreadyPresent
FROM `information_schema`.`COLUMNS`
WHERE `TABLE_SCHEMA` = DATABASE()
  AND `TABLE_NAME` = 'Claims'
  AND `COLUMN_NAME` IN ('ConcurrencyToken', 'DeletedByUserId', 'DeletedUtc', 'IsDeleted');

SELECT
    (SELECT COUNT(*) FROM `Claims`) AS ClaimCount,
    (SELECT COUNT(*) FROM `AspNetUsers`) AS StaffCount;
