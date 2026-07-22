using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace App.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    login = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.login);
                });

            migrationBuilder.CreateTable(
                name: "lots",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_login = table.Column<string>(type: "text", nullable: false),
                    tag_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    initial_price = table.Column<decimal>(type: "numeric", nullable: false),
                    current_price = table.Column<decimal>(type: "numeric", nullable: false),
                    end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    payment_method = table.Column<string>(type: "text", nullable: false),
                    delivery_payment = table.Column<string>(type: "text", nullable: false),
                    leader_login = table.Column<string>(type: "text", nullable: true),
                    closed = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lots", x => x.id);
                    table.ForeignKey(
                        name: "FK_lots_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lots_users_leader_login",
                        column: x => x.leader_login,
                        principalTable: "users",
                        principalColumn: "login");
                    table.ForeignKey(
                        name: "FK_lots_users_user_login",
                        column: x => x.user_login,
                        principalTable: "users",
                        principalColumn: "login",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bids",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_login = table.Column<string>(type: "text", nullable: false),
                    lot_id = table.Column<long>(type: "bigint", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bids", x => x.id);
                    table.ForeignKey(
                        name: "FK_bids_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bids_users_user_login",
                        column: x => x.user_login,
                        principalTable: "users",
                        principalColumn: "login",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lot_images",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lot_id = table.Column<long>(type: "bigint", nullable: false),
                    image_path = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lot_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_lot_images_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchases",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_login = table.Column<string>(type: "text", nullable: false),
                    lot_id = table.Column<long>(type: "bigint", nullable: false),
                    locked_price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchases", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchases_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchases_users_user_login",
                        column: x => x.user_login,
                        principalTable: "users",
                        principalColumn: "login",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bids_lot_id",
                table: "bids",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_bids_user_login",
                table: "bids",
                column: "user_login");

            migrationBuilder.CreateIndex(
                name: "IX_lot_images_lot_id",
                table: "lot_images",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_lots_leader_login",
                table: "lots",
                column: "leader_login");

            migrationBuilder.CreateIndex(
                name: "IX_lots_tag_id",
                table: "lots",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_lots_user_login",
                table: "lots",
                column: "user_login");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_lot_id",
                table: "purchases",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_user_login",
                table: "purchases",
                column: "user_login");

            migrationBuilder.CreateIndex(
                name: "IX_tags_name",
                table: "tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bids");

            migrationBuilder.DropTable(
                name: "lot_images");

            migrationBuilder.DropTable(
                name: "purchases");

            migrationBuilder.DropTable(
                name: "lots");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
