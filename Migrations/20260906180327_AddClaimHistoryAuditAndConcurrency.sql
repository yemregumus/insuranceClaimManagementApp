START TRANSACTION;

ALTER TABLE `Claims` ADD `ConcurrencyToken` varchar(36) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Claims` ADD `DeletedByUserId` varchar(255) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Claims` ADD `DeletedUtc` datetime(6) NULL;

ALTER TABLE `Claims` ADD `IsDeleted` tinyint(1) NOT NULL DEFAULT FALSE;

UPDATE `Claims` SET `ConcurrencyToken` = UUID() WHERE `ConcurrencyToken` IS NULL;

ALTER TABLE `Claims` MODIFY COLUMN `ConcurrencyToken` varchar(36) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Claims` ADD CONSTRAINT `AK_Claims_OrganizationId_Id` UNIQUE (`OrganizationId`, `Id`);

ALTER TABLE `AspNetUsers` ADD CONSTRAINT `AK_AspNetUsers_OrganizationId_Id` UNIQUE (`OrganizationId`, `Id`);

ALTER TABLE `AspNetUsers` DROP INDEX `IX_AspNetUsers_OrganizationId`;

CREATE TABLE `AuditEvents` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `OrganizationId` int NOT NULL,
    `ActorUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `EventType` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `EntityType` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `EntityId` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `OccurredUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_AuditEvents` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AuditEvents_AspNetUsers_OrganizationId_ActorUserId` FOREIGN KEY (`OrganizationId`, `ActorUserId`) REFERENCES `AspNetUsers` (`OrganizationId`, `Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_AuditEvents_Organizations_OrganizationId` FOREIGN KEY (`OrganizationId`) REFERENCES `Organizations` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClaimStatusHistory` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `OrganizationId` int NOT NULL,
    `ClaimId` int NOT NULL,
    `PreviousStatus` varchar(50) CHARACTER SET utf8mb4 NULL,
    `NewStatus` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `ChangedUtc` datetime(6) NOT NULL,
    `ChangedByUserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_ClaimStatusHistory` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ClaimStatusHistory_AspNetUsers_OrganizationId_ChangedByUserId` FOREIGN KEY (`OrganizationId`, `ChangedByUserId`) REFERENCES `AspNetUsers` (`OrganizationId`, `Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ClaimStatusHistory_Claims_OrganizationId_ClaimId` FOREIGN KEY (`OrganizationId`, `ClaimId`) REFERENCES `Claims` (`OrganizationId`, `Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ClaimStatusHistory_Organizations_OrganizationId` FOREIGN KEY (`OrganizationId`) REFERENCES `Organizations` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_AuditEvents_OrganizationId_ActorUserId` ON `AuditEvents` (`OrganizationId`, `ActorUserId`);

CREATE INDEX `IX_AuditEvents_OrganizationId_OccurredUtc` ON `AuditEvents` (`OrganizationId`, `OccurredUtc`);

CREATE INDEX `IX_ClaimStatusHistory_OrganizationId_ChangedByUserId` ON `ClaimStatusHistory` (`OrganizationId`, `ChangedByUserId`);

CREATE INDEX `IX_ClaimStatusHistory_OrganizationId_ClaimId_ChangedUtc` ON `ClaimStatusHistory` (`OrganizationId`, `ClaimId`, `ChangedUtc`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260906180327_AddClaimHistoryAuditAndConcurrency', '8.0.8');

COMMIT;
