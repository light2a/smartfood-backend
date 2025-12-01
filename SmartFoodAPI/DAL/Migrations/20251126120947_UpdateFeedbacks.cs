using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFeedbacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_MenuItems_MenuItemId",
                table: "Feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Restaurants_RestaurantId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "MenuItemId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "Feedbacks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MenuItemId",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "Feedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_MenuItemId",
                table: "Feedbacks",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_RestaurantId",
                table: "Feedbacks",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_MenuItems_MenuItemId",
                table: "Feedbacks",
                column: "MenuItemId",
                principalTable: "MenuItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Restaurants_RestaurantId",
                table: "Feedbacks",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id");
        }
    }
}
