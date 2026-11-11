using api.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.Migrations
{
    // Initial schema: AdmissionClasses and AdmissionHolds with their constraints and indexes.
    // A migration is a recorded "step" that changes the database structure.
    [DbContext(typeof(ApplicationDBContext))]
    [Migration("20261109000000_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TotalSeats = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionClasses", x => x.Id);
                    table.CheckConstraint("CK_AdmissionClasses_TotalSeats_Positive", "\"TotalSeats\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "AdmissionHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionHolds", x => x.Id);
                    table.CheckConstraint("CK_AdmissionHolds_Status", "\"Status\" IN ('HELD','CONFIRMED','EXPIRED','CANCELLED')");
                    table.CheckConstraint("CK_AdmissionHolds_StudentId_Format", "\"StudentId\" ~ '^[0-9]{17}$'");
                    table.ForeignKey(
                        name: "FK_AdmissionHolds_AdmissionClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "AdmissionClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_AdmissionHolds_ClassId", table: "AdmissionHolds", column: "ClassId");
            migrationBuilder.CreateIndex(name: "IX_AdmissionHolds_StudentId", table: "AdmissionHolds", column: "StudentId");
            migrationBuilder.CreateIndex(
                name: "IX_AdmissionHolds_ExpiresAt",
                table: "AdmissionHolds",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(name: "IX_AdmissionHolds_Status", table: "AdmissionHolds", column: "Status");
            migrationBuilder.CreateIndex(
                name: "IX_AdmissionHolds_ClassId_StudentId_Status",
                table: "AdmissionHolds",
                columns: new[] { "ClassId", "StudentId", "Status" });
            migrationBuilder.CreateIndex(
                name: "UX_AdmissionHolds_ActivePerStudentPerClass",
                table: "AdmissionHolds",
                columns: new[] { "ClassId", "StudentId" },
                unique: true,
                filter: "\"Status\" IN ('HELD','CONFIRMED')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AdmissionHolds");
            migrationBuilder.DropTable(name: "AdmissionClasses");
        }
    }
}
