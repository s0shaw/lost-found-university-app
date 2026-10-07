using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversityLostFound.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LostAndFoundModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "university_locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsHandoverPoint = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_university_locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "university_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UniversityId = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_university_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "staff_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UniversityMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_accounts_university_members_UniversityMemberId",
                        column: x => x.UniversityMemberId,
                        principalTable: "university_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingCode = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReporterUniversityMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PublicDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    SecretDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HandoverPointId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HandoverConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReturnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloseReason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_item_reports_university_locations_HandoverPointId",
                        column: x => x.HandoverPointId,
                        principalTable: "university_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_reports_university_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "university_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_reports_university_members_ReporterUniversityMemberId",
                        column: x => x.ReporterUniversityMemberId,
                        principalTable: "university_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_reports_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_questions_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingCode = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    ClaimantUniversityMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LostOn = table.Column<DateOnly>(type: "date", nullable: true),
                    HandoverPointId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StaffNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecidedByStaffId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_claims_university_locations_HandoverPointId",
                        column: x => x.HandoverPointId,
                        principalTable: "university_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_claims_university_members_ClaimantUniversityMemberId",
                        column: x => x.ClaimantUniversityMemberId,
                        principalTable: "university_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_claims_item_reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "item_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claims_staff_accounts_DecidedByStaffId",
                        column: x => x.DecidedByStaffId,
                        principalTable: "staff_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_report_secret_answers",
                columns: table => new
                {
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_report_secret_answers", x => new { x.ItemReportId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_item_report_secret_answers_item_reports_ItemReportId",
                        column: x => x.ItemReportId,
                        principalTable: "item_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_question_options_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "claim_secret_answers",
                columns: table => new
                {
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_secret_answers", x => new { x.ClaimId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_claim_secret_answers_claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_university_locations_Name",
                table: "university_locations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_university_members_UniversityId",
                table: "university_members",
                column: "UniversityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name",
                table: "categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_claims_ClaimantUniversityMemberId",
                table: "claims",
                column: "ClaimantUniversityMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_claims_DecidedByStaffId",
                table: "claims",
                column: "DecidedByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_claims_HandoverPointId",
                table: "claims",
                column: "HandoverPointId");

            migrationBuilder.CreateIndex(
                name: "IX_claims_ReportId_ClaimantUniversityMemberId",
                table: "claims",
                columns: new[] { "ReportId", "ClaimantUniversityMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_claims_Status_Score",
                table: "claims",
                columns: new[] { "Status", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_claims_TrackingCode",
                table: "claims",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_reports_CategoryId",
                table: "item_reports",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_item_reports_HandoverPointId",
                table: "item_reports",
                column: "HandoverPointId");

            migrationBuilder.CreateIndex(
                name: "IX_item_reports_LocationId",
                table: "item_reports",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_item_reports_ReporterUniversityMemberId",
                table: "item_reports",
                column: "ReporterUniversityMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_item_reports_TrackingCode",
                table: "item_reports",
                column: "TrackingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_item_reports_Type_Status_OccurredOn",
                table: "item_reports",
                columns: new[] { "Type", "Status", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_question_options_QuestionId",
                table: "question_options",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_questions_CategoryId",
                table: "questions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_accounts_UniversityMemberId",
                table: "staff_accounts",
                column: "UniversityMemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "claim_secret_answers");

            migrationBuilder.DropTable(
                name: "item_report_secret_answers");

            migrationBuilder.DropTable(
                name: "question_options");

            migrationBuilder.DropTable(
                name: "claims");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "item_reports");

            migrationBuilder.DropTable(
                name: "staff_accounts");

            migrationBuilder.DropTable(
                name: "university_locations");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "university_members");
        }
    }
}
