using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FraudEngine.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRichDomainV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "Transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "CardLast4",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Channel",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantCategoryCode",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantId",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantName",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Alerts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "Alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "Alerts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Alerts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "text", nullable: false),
                    AccountType = table.Column<int>(type: "integer", nullable: false),
                    RiskTier = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    DefaultCountryCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Channel",
                table: "Transactions",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CountryCode",
                table: "Transactions",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_MerchantId",
                table: "Transactions",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountId",
                table: "Accounts",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Channel",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_CountryCode",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_MerchantId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CardLast4",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "MerchantCategoryCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "MerchantName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Alerts");

            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Alerts",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
