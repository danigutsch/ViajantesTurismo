using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Discount_Amount",
                table: "Booking",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discount_Reason",
                table: "Booking",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discount_Type",
                table: "Booking",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discount_Amount",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "Discount_Reason",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "Discount_Type",
                table: "Booking");
        }
    }
}
