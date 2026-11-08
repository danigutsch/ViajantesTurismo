using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TourCurrencyIsoCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF cannot infer this data transform: rewrite the former enum names to ISO 4217 codes before narrowing the column.
            migrationBuilder.Sql(
                """
                UPDATE "Tours"
                SET "Currency" = CASE "Currency"
                    WHEN 'Real' THEN 'BRL'
                    WHEN 'Euro' THEN 'EUR'
                    WHEN 'UsDollar' THEN 'USD'
                END
                WHERE "Currency" IN ('Real', 'Euro', 'UsDollar');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Tours",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Tours",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.Sql(
                """
                UPDATE "Tours"
                SET "Currency" = CASE "Currency"
                    WHEN 'BRL' THEN 'Real'
                    WHEN 'EUR' THEN 'Euro'
                    WHEN 'USD' THEN 'UsDollar'
                END
                WHERE "Currency" IN ('BRL', 'EUR', 'USD');
                """);
        }
    }
}
