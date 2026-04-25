using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProducScan.Migrations
{
    /// <inheritdoc />
    public partial class AddImagenYActivoToMandril : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Imagen",
                schema: "dbo",
                table: "Mandriles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LTester",
                schema: "dbo",
                table: "Mandriles",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Imagen",
                schema: "dbo",
                table: "Mandriles");

            migrationBuilder.DropColumn(
                name: "LTester",
                schema: "dbo",
                table: "Mandriles");
        }
    }
}
