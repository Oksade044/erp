using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalDepositSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DamageCharge",
                table: "Orders",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DamageCurrency",
                table: "Orders",
                type: "TEXT",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Deposit",
                table: "Orders",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositCurrency",
                table: "Orders",
                type: "TEXT",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSettled",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PenaltyCharge",
                table: "Orders",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PenaltyCurrency",
                table: "Orders",
                type: "TEXT",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementNotes",
                table: "Orders",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DamageCharge",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DamageCurrency",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Deposit",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DepositCurrency",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsSettled",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PenaltyCharge",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PenaltyCurrency",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SettlementNotes",
                table: "Orders");
        }
    }
}
