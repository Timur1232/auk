using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class remove_lot_name_title_prefered : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "lots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "lots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
