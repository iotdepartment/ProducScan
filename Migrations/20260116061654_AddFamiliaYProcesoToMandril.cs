using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProducScan.Migrations
{
    public partial class AddFamiliaYProcesoToMandril : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Familia",
                table: "Mandriles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Proceso",
                table: "Mandriles",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Familia",
                table: "Mandriles");

            migrationBuilder.DropColumn(
                name: "Proceso",
                table: "Mandriles");
        }
    }
}