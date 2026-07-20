using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class add_lot_leader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "leader_login",
                table: "lots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lots_leader_login",
                table: "lots",
                column: "leader_login");

            migrationBuilder.AddForeignKey(
                name: "FK_lots_users_leader_login",
                table: "lots",
                column: "leader_login",
                principalTable: "users",
                principalColumn: "login");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lots_users_leader_login",
                table: "lots");

            migrationBuilder.DropIndex(
                name: "IX_lots_leader_login",
                table: "lots");

            migrationBuilder.DropColumn(
                name: "leader_login",
                table: "lots");
        }
    }
}
