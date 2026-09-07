using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceClaimManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimHistoryAuditAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyToken",
                table: "Claims",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "Claims",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedUtc",
                table: "Claims",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Claims",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE `Claims` SET `ConcurrencyToken` = UUID() WHERE `ConcurrencyToken` IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyToken",
                table: "Claims",
                type: "varchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldMaxLength: 36,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Claims_OrganizationId_Id",
                table: "Claims",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AspNetUsers_OrganizationId_Id",
                table: "AspNetUsers",
                columns: new[] { "OrganizationId", "Id" });

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_AspNetUsers_OrganizationId_ActorUserId",
                        columns: x => new { x.OrganizationId, x.ActorUserId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClaimStatusHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    ClaimId = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimStatusHistory_AspNetUsers_OrganizationId_ChangedByUserId",
                        columns: x => new { x.OrganizationId, x.ChangedByUserId },
                        principalTable: "AspNetUsers",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimStatusHistory_Claims_OrganizationId_ClaimId",
                        columns: x => new { x.OrganizationId, x.ClaimId },
                        principalTable: "Claims",
                        principalColumns: new[] { "OrganizationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimStatusHistory_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OrganizationId_ActorUserId",
                table: "AuditEvents",
                columns: new[] { "OrganizationId", "ActorUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OrganizationId_OccurredUtc",
                table: "AuditEvents",
                columns: new[] { "OrganizationId", "OccurredUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimStatusHistory_OrganizationId_ChangedByUserId",
                table: "ClaimStatusHistory",
                columns: new[] { "OrganizationId", "ChangedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimStatusHistory_OrganizationId_ClaimId_ChangedUtc",
                table: "ClaimStatusHistory",
                columns: new[] { "OrganizationId", "ClaimId", "ChangedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "ClaimStatusHistory");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Claims_OrganizationId_Id",
                table: "Claims");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AspNetUsers_OrganizationId_Id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ConcurrencyToken",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "DeletedUtc",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Claims");

        }
    }
}
