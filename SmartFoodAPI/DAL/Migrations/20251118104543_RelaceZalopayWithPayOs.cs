using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class RelaceZalopayWithPayOs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsZaloPayOnboardingCompleted",
                table: "Sellers");

            migrationBuilder.RenameColumn(
                name: "ZaloPayMerchantId",
                table: "Sellers",
                newName: "BankCode");

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Sellers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Sellers");

            migrationBuilder.RenameColumn(
                name: "BankCode",
                table: "Sellers",
                newName: "ZaloPayMerchantId");

            migrationBuilder.AddColumn<bool>(
                name: "IsZaloPayOnboardingCompleted",
                table: "Sellers",
                type: "bit",
                nullable: true);
        }
    }
}
