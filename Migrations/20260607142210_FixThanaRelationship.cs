using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BloodDonationManagement.Migrations
{
    /// <inheritdoc />
    public partial class FixThanaRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donors_Districts_DistrictId",
                table: "Donors");

            migrationBuilder.DropForeignKey(
                name: "FK_Donors_Thanas_ThanaId1",
                table: "Donors");

            migrationBuilder.DropForeignKey(
                name: "FK_Thanas_Districts_DistrictId",
                table: "Thanas");

            migrationBuilder.DropIndex(
                name: "IX_Donors_ThanaId1",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "ThanaId1",
                table: "Donors");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Donors",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Donors_Districts_DistrictId",
                table: "Donors",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "DistrictId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Thanas_Districts_DistrictId",
                table: "Thanas",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "DistrictId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Donors_Districts_DistrictId",
                table: "Donors");

            migrationBuilder.DropForeignKey(
                name: "FK_Thanas_Districts_DistrictId",
                table: "Thanas");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Donors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "ThanaId1",
                table: "Donors",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Donors_ThanaId1",
                table: "Donors",
                column: "ThanaId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Donors_Districts_DistrictId",
                table: "Donors",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "DistrictId");

            migrationBuilder.AddForeignKey(
                name: "FK_Donors_Thanas_ThanaId1",
                table: "Donors",
                column: "ThanaId1",
                principalTable: "Thanas",
                principalColumn: "ThanaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Thanas_Districts_DistrictId",
                table: "Thanas",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "DistrictId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
