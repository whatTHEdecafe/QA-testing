using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QaAutomation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSafeScannerFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StartingUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    FinalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    PageTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BrowserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ViewportWidth = table.Column<int>(type: "int", nullable: false),
                    ViewportHeight = table.Column<int>(type: "int", nullable: false),
                    DetectedPageCount = table.Column<int>(type: "int", nullable: false),
                    DetectedElementCount = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    FailureSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CancellationRequested = table.Column<bool>(type: "bit", nullable: false),
                    CancellationRequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scans_Targets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScanDiagnostics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanDiagnostics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanDiagnostics_Scans_ScanId",
                        column: x => x.ScanId,
                        principalTable: "Scans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScannedPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    FinalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    OriginalPageTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MainHeading = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GeneratedDisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DiscoveryOrder = table.Column<int>(type: "int", nullable: false),
                    ScreenshotPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScreenshotWidth = table.Column<int>(type: "int", nullable: true),
                    ScreenshotHeight = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScannedPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScannedPages_Scans_ScanId",
                        column: x => x.ScanId,
                        principalTable: "Scans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetectedElements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscoveryOrder = table.Column<int>(type: "int", nullable: false),
                    TagName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InputType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AccessibleRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccessibleName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VisibleText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssociatedLabel = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NameAttribute = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HtmlId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TestId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActionable = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsPotentiallyDestructive = table.Column<bool>(type: "bit", nullable: false),
                    BoundingX = table.Column<double>(type: "float", nullable: true),
                    BoundingY = table.Column<double>(type: "float", nullable: true),
                    BoundingWidth = table.Column<double>(type: "float", nullable: true),
                    BoundingHeight = table.Column<double>(type: "float", nullable: true),
                    CropPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScreenshotError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedElements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectedElements_ScannedPages_PageId",
                        column: x => x.PageId,
                        principalTable: "ScannedPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SelectorCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelectorType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SelectorValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    WasUnique = table.Column<bool>(type: "bit", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    IsPreferred = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectorCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelectorCandidates_DetectedElements_ElementId",
                        column: x => x.ElementId,
                        principalTable: "DetectedElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetectedElements_PageId_DiscoveryOrder",
                table: "DetectedElements",
                columns: new[] { "PageId", "DiscoveryOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ScanDiagnostics_ScanId_CreatedAtUtc",
                table: "ScanDiagnostics",
                columns: new[] { "ScanId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ScannedPages_ScanId_DiscoveryOrder",
                table: "ScannedPages",
                columns: new[] { "ScanId", "DiscoveryOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Scans_RequestedAtUtc",
                table: "Scans",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_Status",
                table: "Scans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_TargetId",
                table: "Scans",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_TargetId_Status",
                table: "Scans",
                columns: new[] { "TargetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SelectorCandidates_ElementId_Priority",
                table: "SelectorCandidates",
                columns: new[] { "ElementId", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanDiagnostics");

            migrationBuilder.DropTable(
                name: "SelectorCandidates");

            migrationBuilder.DropTable(
                name: "DetectedElements");

            migrationBuilder.DropTable(
                name: "ScannedPages");

            migrationBuilder.DropTable(
                name: "Scans");
        }
    }
}
