using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplierLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DocumentPath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierLedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierLedgerEntries_SupplierId",
                table: "SupplierLedgerEntries",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierLedgerEntries");
        }
    }
}
