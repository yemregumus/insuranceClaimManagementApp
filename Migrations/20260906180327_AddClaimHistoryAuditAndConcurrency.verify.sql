-- Run after 20260906180327_AddClaimHistoryAuditAndConcurrency.sql.

SELECT COUNT(*) AS NewMigrationApplied
FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20260906180327_AddClaimHistoryAuditAndConcurrency';

SELECT COUNT(*) AS NewTablesPresent
FROM `information_schema`.`TABLES`
WHERE `TABLE_SCHEMA` = DATABASE()
  AND `TABLE_NAME` IN ('AuditEvents', 'ClaimStatusHistory');

SELECT COUNT(*) AS NewClaimColumnsPresent
FROM `information_schema`.`COLUMNS`
WHERE `TABLE_SCHEMA` = DATABASE()
  AND `TABLE_NAME` = 'Claims'
  AND `COLUMN_NAME` IN ('ConcurrencyToken', 'DeletedByUserId', 'DeletedUtc', 'IsDeleted');

SELECT
    SUM(CASE WHEN `ConcurrencyToken` IS NULL OR `ConcurrencyToken` = '' THEN 1 ELSE 0 END) AS ClaimsWithoutConcurrencyToken,
    SUM(CASE WHEN `IsDeleted` = 1 THEN 1 ELSE 0 END) AS InitiallyArchivedClaims
FROM `Claims`;

SELECT COUNT(*) AS DuplicateConcurrencyTokenGroups
FROM (
    SELECT `ConcurrencyToken`
    FROM `Claims`
    GROUP BY `ConcurrencyToken`
    HAVING COUNT(*) > 1
) AS duplicates;

SELECT
    (SELECT COUNT(*) FROM `AuditEvents`) AS InitialAuditEventCount,
    (SELECT COUNT(*) FROM `ClaimStatusHistory`) AS InitialStatusHistoryCount;
