using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobsApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalJobsAndResumes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add CompanyName column to Users
            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // 2. Create InternalJobs table
            migrationBuilder.CreateTable(
                name: "InternalJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Remote = table.Column<bool>(type: "boolean", nullable: false),
                    EmploymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SalaryMin = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    SalaryMax = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    SalaryCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    TagsCsv = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    ApplicationUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApplicationInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApplicationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalJobs_Users_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InternalJobs_EmployerId",
                table: "InternalJobs",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalJobs_Status",
                table: "InternalJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InternalJobs_Status_ApplicationDeadline",
                table: "InternalJobs",
                columns: new[] { "Status", "ApplicationDeadline" });

            // 3. Create Resumes table
            migrationBuilder.CreateTable(
                name: "Resumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Headline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExperienceJson = table.Column<string>(type: "jsonb", nullable: false),
                    EducationJson = table.Column<string>(type: "jsonb", nullable: false),
                    SkillsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resumes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resumes_UserId",
                table: "Resumes",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "InternalJobs");
            migrationBuilder.DropTable(name: "Resumes");
            migrationBuilder.DropColumn(name: "CompanyName", table: "Users");
        }
    }
}
