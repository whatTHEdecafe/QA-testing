using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QaAutomation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScannerReviewTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActionTimeoutMilliseconds",
                table: "Scans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElementScreenshotPadding",
                table: "Scans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaximumDetectedElements",
                table: "Scans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaximumDiagnosticRecords",
                table: "Scans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NavigationTimeoutMilliseconds",
                table: "Scans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OverallTimeoutSeconds",
                table: "Scans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewUpdatedAtUtc",
                table: "ScannedPages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserDisplayName",
                table: "ScannedPages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassificationOverride",
                table: "DetectedElements",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ManualPreferredSelectorCandidateId",
                table: "DetectedElements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewUpdatedAtUtc",
                table: "DetectedElements",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserDisplayName",
                table: "DetectedElements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scans_PageTitle",
                table: "Scans",
                column: "PageTitle");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_Status_RequestedAtUtc",
                table: "Scans",
                columns: new[] { "Status", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ScannedPages_ReviewUpdatedAtUtc",
                table: "ScannedPages",
                column: "ReviewUpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ScannedPages_UserDisplayName",
                table: "ScannedPages",
                column: "UserDisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_ScanDiagnostics_ScanId_Category",
                table: "ScanDiagnostics",
                columns: new[] { "ScanId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_ScanDiagnostics_ScanId_Severity",
                table: "ScanDiagnostics",
                columns: new[] { "ScanId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_ScanDiagnostics_StatusCode",
                table: "ScanDiagnostics",
                column: "StatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedElements_ClassificationOverride",
                table: "DetectedElements",
                column: "ClassificationOverride");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedElements_IsPotentiallyDestructive",
                table: "DetectedElements",
                column: "IsPotentiallyDestructive");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedElements_ManualPreferredSelectorCandidateId",
                table: "DetectedElements",
                column: "ManualPreferredSelectorCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedElements_UserDisplayName",
                table: "DetectedElements",
                column: "UserDisplayName");

            migrationBuilder.AddForeignKey(
                name: "FK_DetectedElements_SelectorCandidates_ManualPreferredSelectorCandidateId",
                table: "DetectedElements",
                column: "ManualPreferredSelectorCandidateId",
                principalTable: "SelectorCandidates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetectedElements_SelectorCandidates_ManualPreferredSelectorCandidateId",
                table: "DetectedElements");

            migrationBuilder.DropIndex(
                name: "IX_Scans_PageTitle",
                table: "Scans");

            migrationBuilder.DropIndex(
                name: "IX_Scans_Status_RequestedAtUtc",
                table: "Scans");

            migrationBuilder.DropIndex(
                name: "IX_ScannedPages_ReviewUpdatedAtUtc",
                table: "ScannedPages");

            migrationBuilder.DropIndex(
                name: "IX_ScannedPages_UserDisplayName",
                table: "ScannedPages");

            migrationBuilder.DropIndex(
                name: "IX_ScanDiagnostics_ScanId_Category",
                table: "ScanDiagnostics");

            migrationBuilder.DropIndex(
                name: "IX_ScanDiagnostics_ScanId_Severity",
                table: "ScanDiagnostics");

            migrationBuilder.DropIndex(
                name: "IX_ScanDiagnostics_StatusCode",
                table: "ScanDiagnostics");

            migrationBuilder.DropIndex(
                name: "IX_DetectedElements_ClassificationOverride",
                table: "DetectedElements");

            migrationBuilder.DropIndex(
                name: "IX_DetectedElements_IsPotentiallyDestructive",
                table: "DetectedElements");

            migrationBuilder.DropIndex(
                name: "IX_DetectedElements_ManualPreferredSelectorCandidateId",
                table: "DetectedElements");

            migrationBuilder.DropIndex(
                name: "IX_DetectedElements_UserDisplayName",
                table: "DetectedElements");

            migrationBuilder.DropColumn(
                name: "ActionTimeoutMilliseconds",
                table: "Scans");

            migrationBuilder.DropColumn(
                name: "ElementScreenshotPadding",
                table: "Scans");

            migrationBuilder.DropColumn(
                name: "MaximumDetectedElements",
                table: "Scans");

            migrationBuilder.DropColumn(
                name: "MaximumDiagnosticRecords",
                table: "Scans");

            migrationBuilder.DropColumn(
                name: "NavigationTimeoutMilliseconds",
                table: "Scans");

            migrationBuilder.DropColumn(
                name: "OverallTimeoutSeconds",
                table: "Scans");

            migrationBuilder.DropColumn(
                name: "ReviewUpdatedAtUtc",
                table: "ScannedPages");

            migrationBuilder.DropColumn(
                name: "UserDisplayName",
                table: "ScannedPages");

            migrationBuilder.DropColumn(
                name: "ClassificationOverride",
                table: "DetectedElements");

            migrationBuilder.DropColumn(
                name: "ManualPreferredSelectorCandidateId",
                table: "DetectedElements");

            migrationBuilder.DropColumn(
                name: "ReviewUpdatedAtUtc",
                table: "DetectedElements");

            migrationBuilder.DropColumn(
                name: "UserDisplayName",
                table: "DetectedElements");
        }
    }
}
