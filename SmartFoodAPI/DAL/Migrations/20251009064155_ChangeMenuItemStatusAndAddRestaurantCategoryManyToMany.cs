using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMenuItemStatusAndAddRestaurantCategoryManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Restaurants_Categories_CategoryId",
                table: "Restaurants");

            migrationBuilder.DropIndex(
                name: "IX_Restaurants_CategoryId",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Restaurants");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "MenuItems");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "MenuItems",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "RestaurantCategory",
                columns: table => new
                {
                    RestaurantId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantCategory", x => new { x.RestaurantId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_RestaurantCategory_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantCategory_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCategory_CategoryId",
                table: "RestaurantCategory",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantCategory");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MenuItems");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Restaurants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_CategoryId",
                table: "Restaurants",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Restaurants_Categories_CategoryId",
                table: "Restaurants",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
