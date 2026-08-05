using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarketConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductDirectListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_Users_ReceiverId",
                table: "ChatMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_Users_SenderId",
                table: "ChatMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishlist_Products_ProductId",
                table: "Wishlist");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishlist_Users_BuyerId",
                table: "Wishlist");

            migrationBuilder.DropTable(
                name: "MerchantProfile");

            migrationBuilder.DropTable(
                name: "StallProduct");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Wishlist",
                table: "Wishlist");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage");

            migrationBuilder.RenameTable(
                name: "Wishlist",
                newName: "Wishlists");

            migrationBuilder.RenameTable(
                name: "ChatMessage",
                newName: "ChatMessages");

            migrationBuilder.RenameIndex(
                name: "IX_Wishlist_ProductId",
                table: "Wishlists",
                newName: "IX_Wishlists_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Wishlist_BuyerId",
                table: "Wishlists",
                newName: "IX_Wishlists_BuyerId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessage_SenderId",
                table: "ChatMessages",
                newName: "IX_ChatMessages_SenderId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessage_ReceiverId",
                table: "ChatMessages",
                newName: "IX_ChatMessages_ReceiverId");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Products",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsFree",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrls",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SellerType",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubCategory",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Warranty",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Wishlists",
                table: "Wishlists",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessages",
                table: "ChatMessages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UserId",
                table: "Products",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users_ReceiverId",
                table: "ChatMessages",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users_SenderId",
                table: "ChatMessages",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Users_UserId",
                table: "Products",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_Products_ProductId",
                table: "Wishlists",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_Users_BuyerId",
                table: "Wishlists",
                column: "BuyerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Users_ReceiverId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Users_SenderId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Users_UserId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_Products_ProductId",
                table: "Wishlists");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_Users_BuyerId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Products_UserId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Wishlists",
                table: "Wishlists");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessages",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsFree",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MediaUrls",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SellerType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SubCategory",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Warranty",
                table: "Products");

            migrationBuilder.RenameTable(
                name: "Wishlists",
                newName: "Wishlist");

            migrationBuilder.RenameTable(
                name: "ChatMessages",
                newName: "ChatMessage");

            migrationBuilder.RenameIndex(
                name: "IX_Wishlists_ProductId",
                table: "Wishlist",
                newName: "IX_Wishlist_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_Wishlists_BuyerId",
                table: "Wishlist",
                newName: "IX_Wishlist_BuyerId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessages_SenderId",
                table: "ChatMessage",
                newName: "IX_ChatMessage_SenderId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessages_ReceiverId",
                table: "ChatMessage",
                newName: "IX_ChatMessage_ReceiverId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Wishlist",
                table: "Wishlist",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessage",
                table: "ChatMessage",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "MerchantProfile",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MarketName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StallName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StallNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantProfile", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_MerchantProfile_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StallProduct",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    IsTrending = table.Column<bool>(type: "boolean", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StallProduct", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StallProduct_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StallProduct_Users_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StallProduct_MerchantId",
                table: "StallProduct",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_StallProduct_ProductId",
                table: "StallProduct",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_Users_ReceiverId",
                table: "ChatMessage",
                column: "ReceiverId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_Users_SenderId",
                table: "ChatMessage",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlist_Products_ProductId",
                table: "Wishlist",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlist_Users_BuyerId",
                table: "Wishlist",
                column: "BuyerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
