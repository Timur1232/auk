using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class add_correct_foreign_keys_again : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LotLotImage");

            migrationBuilder.CreateIndex(
                name: "IX_lot_images_lot_id",
                table: "lot_images",
                column: "lot_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lot_images_lots_lot_id",
                table: "lot_images",
                column: "lot_id",
                principalTable: "lots",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lot_images_lots_lot_id",
                table: "lot_images");

            migrationBuilder.DropIndex(
                name: "IX_lot_images_lot_id",
                table: "lot_images");

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
                name: "IX_LotLotImage_lotsid",
                table: "LotLotImage",
                column: "lotsid");
        }
    }
}
