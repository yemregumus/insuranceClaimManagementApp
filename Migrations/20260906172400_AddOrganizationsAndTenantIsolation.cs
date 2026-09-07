using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceClaimManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationsAndTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Claims_Users_UserId",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Claims_UserId",
                table: "Claims");

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "Name", "Slug", "IsActive", "CreatedUtc" },
                values: new object[] { 1, "Default Organization", "default", true, new DateTime(2026, 9, 6, 17, 24, 0, DateTimeKind.Utc) });

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Claims",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedUtc",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.Sql("UPDATE `Users` SET `OrganizationId` = 1 WHERE `OrganizationId` IS NULL;");
            migrationBuilder.Sql("UPDATE `Claims` SET `OrganizationId` = 1 WHERE `OrganizationId` IS NULL;");
            migrationBuilder.Sql(
                "UPDATE `AspNetUsers` SET `OrganizationId` = 1, `DisplayName` = COALESCE(NULLIF(`UserName`, ''), `Email`, 'Staff'), `IsActive` = 1, `CreatedUtc` = UTC_TIMESTAMP(6) WHERE `OrganizationId` IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "Claims",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedUtc",
                table: "AspNetUsers",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OrganizationId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Users_OrganizationId_Id",
                table: "Users",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId_Email",
                table: "Users",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId_Username",
                table: "Users",
                columns: new[] { "OrganizationId", "Username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Claims_OrganizationId_UserId",
                table: "Claims",
                columns: new[] { "OrganizationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_Organizations_OrganizationId",
                table: "Claims",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_Users_OrganizationId_UserId",
                table: "Claims",
                columns: new[] { "OrganizationId", "UserId" },
                principalTable: "Users",
                principalColumns: new[] { "OrganizationId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Claims_Organizations_OrganizationId",
                table: "Claims");

            migrationBuilder.DropForeignKey(
                name: "FK_Claims_Users_OrganizationId_UserId",
                table: "Claims");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Users_OrganizationId_Id",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_OrganizationId_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_OrganizationId_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Claims_OrganizationId_UserId",
                table: "Claims");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedUtc",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Claims_UserId",
                table: "Claims",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Claims_Users_UserId",
                table: "Claims",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
