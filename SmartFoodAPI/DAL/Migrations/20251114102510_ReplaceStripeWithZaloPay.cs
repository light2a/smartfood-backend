using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStripeWithZaloPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStripeOnboardingCompleted",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                table: "Account");

            migrationBuilder.RenameColumn(
                name: "StripeAccountId",
                table: "Sellers",
                newName: "ZaloPayMerchantId");

            migrationBuilder.AddColumn<bool>(
                name: "IsZaloPayOnboardingCompleted",
                table: "Sellers",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsZaloPayOnboardingCompleted",
                table: "Sellers");

            migrationBuilder.RenameColumn(
                name: "ZaloPayMerchantId",
                table: "Sellers",
                newName: "StripeAccountId");

            migrationBuilder.AddColumn<bool>(
                name: "IsStripeOnboardingCompleted",
                table: "Sellers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                table: "Account",
                type: "bit",
                nullable: true);
        }
    }
}
