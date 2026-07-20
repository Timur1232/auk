using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class add_bets_and_update_lots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "delivery_payment",
                table: "lots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "lots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "lots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "payment_method",
                table: "lots",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "bets",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_login = table.Column<string>(type: "TEXT", nullable: false),
                    lot_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bets", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bets");

            migrationBuilder.DropColumn(
                name: "delivery_payment",
                table: "lots");

            migrationBuilder.DropColumn(
                name: "description",
                table: "lots");

            migrationBuilder.DropColumn(
                name: "location",
                table: "lots");

            migrationBuilder.DropColumn(
                name: "payment_method",
                table: "lots");
        }
    }
}
