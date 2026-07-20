using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class add_correct_foreign_keys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "bets",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "LotLotImage",
                columns: table => new
                {
                    imagesid = table.Column<uint>(type: "INTEGER", nullable: false),
                    lotsid = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotLotImage", x => new { x.imagesid, x.lotsid });
                    table.ForeignKey(
                        name: "FK_LotLotImage_lot_images_imagesid",
                        column: x => x.imagesid,
                        principalTable: "lot_images",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LotLotImage_lots_lotsid",
                        column: x => x.lotsid,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lots_tag_id",
                table: "lots",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_lots_user_login",
                table: "lots",
                column: "user_login");

            migrationBuilder.CreateIndex(
                name: "IX_bets_lot_id",
                table: "bets",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_bets_user_login",
                table: "bets",
                column: "user_login");

            migrationBuilder.CreateIndex(
                name: "IX_LotLotImage_lotsid",
                table: "LotLotImage",
                column: "lotsid");

            migrationBuilder.AddForeignKey(
                name: "FK_bets_lots_lot_id",
                table: "bets",
                column: "lot_id",
                principalTable: "lots",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bets_users_user_login",
                table: "bets",
                column: "user_login",
                principalTable: "users",
                principalColumn: "login",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lots_tags_tag_id",
                table: "lots",
                column: "tag_id",
                principalTable: "tags",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lots_users_user_login",
                table: "lots",
                column: "user_login",
                principalTable: "users",
                principalColumn: "login",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bets_lots_lot_id",
                table: "bets");

            migrationBuilder.DropForeignKey(
                name: "FK_bets_users_user_login",
                table: "bets");

            migrationBuilder.DropForeignKey(
                name: "FK_lots_tags_tag_id",
                table: "lots");

            migrationBuilder.DropForeignKey(
                name: "FK_lots_users_user_login",
                table: "lots");

            migrationBuilder.DropTable(
                name: "LotLotImage");

            migrationBuilder.DropIndex(
                name: "IX_lots_tag_id",
                table: "lots");

            migrationBuilder.DropIndex(
                name: "IX_lots_user_login",
                table: "lots");

            migrationBuilder.DropIndex(
                name: "IX_bets_lot_id",
                table: "bets");

            migrationBuilder.DropIndex(
                name: "IX_bets_user_login",
                table: "bets");

            migrationBuilder.DropColumn(
                name: "price",
                table: "bets");
        }
    }
}
