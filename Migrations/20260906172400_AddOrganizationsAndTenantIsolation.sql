START TRANSACTION;

ALTER TABLE `Claims` DROP FOREIGN KEY `FK_Claims_Users_UserId`;

ALTER TABLE `Users` DROP INDEX `Email`;

ALTER TABLE `Claims` DROP INDEX `IX_Claims_UserId`;

CREATE TABLE `Organizations` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Slug` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `CreatedUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_Organizations` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

INSERT INTO `Organizations` (`Id`, `Name`, `Slug`, `IsActive`, `CreatedUtc`)
VALUES (1, 'Default Organization', 'default', TRUE, TIMESTAMP '2026-09-06 17:24:00');

ALTER TABLE `Users` ADD `OrganizationId` int NULL;

ALTER TABLE `Claims` ADD `OrganizationId` int NULL;

ALTER TABLE `AspNetUsers` ADD `CreatedUtc` datetime(6) NULL;

ALTER TABLE `AspNetUsers` ADD `DisplayName` varchar(200) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `AspNetUsers` ADD `IsActive` tinyint(1) NULL;

ALTER TABLE `AspNetUsers` ADD `OrganizationId` int NULL;

ALTER TABLE `AspNetUsers` ADD `UpdatedUtc` datetime(6) NULL;

UPDATE `Users` SET `OrganizationId` = 1 WHERE `OrganizationId` IS NULL;

UPDATE `Claims` SET `OrganizationId` = 1 WHERE `OrganizationId` IS NULL;

UPDATE `AspNetUsers` SET `OrganizationId` = 1, `DisplayName` = COALESCE(NULLIF(`UserName`, ''), `Email`, 'Staff'), `IsActive` = 1, `CreatedUtc` = UTC_TIMESTAMP(6) WHERE `OrganizationId` IS NULL;

ALTER TABLE `Users` MODIFY COLUMN `OrganizationId` int NOT NULL;

ALTER TABLE `Claims` MODIFY COLUMN `OrganizationId` int NOT NULL;

ALTER TABLE `AspNetUsers` MODIFY COLUMN `CreatedUtc` datetime(6) NOT NULL;

ALTER TABLE `AspNetUsers` MODIFY COLUMN `DisplayName` varchar(200) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `AspNetUsers` MODIFY COLUMN `IsActive` tinyint(1) NOT NULL;

ALTER TABLE `AspNetUsers` MODIFY COLUMN `OrganizationId` int NOT NULL;

ALTER TABLE `Users` ADD CONSTRAINT `AK_Users_OrganizationId_Id` UNIQUE (`OrganizationId`, `Id`);

CREATE UNIQUE INDEX `IX_Users_OrganizationId_Email` ON `Users` (`OrganizationId`, `Email`);

CREATE UNIQUE INDEX `IX_Users_OrganizationId_Username` ON `Users` (`OrganizationId`, `Username`);

CREATE INDEX `IX_Claims_OrganizationId_UserId` ON `Claims` (`OrganizationId`, `UserId`);

CREATE INDEX `IX_AspNetUsers_OrganizationId` ON `AspNetUsers` (`OrganizationId`);

CREATE UNIQUE INDEX `IX_Organizations_Slug` ON `Organizations` (`Slug`);

ALTER TABLE `AspNetUsers` ADD CONSTRAINT `FK_AspNetUsers_Organizations_OrganizationId` FOREIGN KEY (`OrganizationId`) REFERENCES `Organizations` (`Id`) ON DELETE RESTRICT;

ALTER TABLE `Claims` ADD CONSTRAINT `FK_Claims_Organizations_OrganizationId` FOREIGN KEY (`OrganizationId`) REFERENCES `Organizations` (`Id`) ON DELETE RESTRICT;

ALTER TABLE `Claims` ADD CONSTRAINT `FK_Claims_Users_OrganizationId_UserId` FOREIGN KEY (`OrganizationId`, `UserId`) REFERENCES `Users` (`OrganizationId`, `Id`) ON DELETE RESTRICT;

ALTER TABLE `Users` ADD CONSTRAINT `FK_Users_Organizations_OrganizationId` FOREIGN KEY (`OrganizationId`) REFERENCES `Organizations` (`Id`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260906172400_AddOrganizationsAndTenantIsolation', '8.0.8');

COMMIT;
