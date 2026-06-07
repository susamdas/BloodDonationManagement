using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodDonationManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientNameToBloodRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PatientName",
                table: "BloodRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatientName",
                table: "BloodRequests");
        }
    }
}
