using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProducScan.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Logs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Logs");
        }
    }
}
