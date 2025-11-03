using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTotalPriceColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Booking_Customers_CompanionId",
                table: "Booking");

            migrationBuilder.DropForeignKey(
                name: "FK_Booking_Customers_CustomerId",
                table: "Booking");

            migrationBuilder.RenameColumn(
                name: "SingleRoomSupplementPrice",
                table: "Tours",
                newName: "DoubleRoomSupplementPrice");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Booking",
                newName: "PrincipalCustomer_CustomerId");

            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                table: "Booking",
                newName: "RoomAdditionalCost");

            migrationBuilder.RenameColumn(
                name: "CompanionId",
                table: "Booking",
                newName: "CompanionCustomer_CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_Booking_CustomerId",
                table: "Booking",
                newName: "IX_Booking_PrincipalCustomer_CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_Booking_CompanionId",
                table: "Booking",
                newName: "IX_Booking_CompanionCustomer_CustomerId");

            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                table: "Booking",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CompanionCustomer_BikePrice",
                table: "Booking",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanionCustomer_BikeType",
                table: "Booking",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrincipalCustomer_BikePrice",
                table: "Booking",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PrincipalCustomer_BikeType",
                table: "Booking",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoomType",
                table: "Booking",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Customers_CompanionCustomer_CustomerId",
                table: "Booking",
                column: "CompanionCustomer_CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Customers_PrincipalCustomer_CustomerId",
                table: "Booking",
                column: "PrincipalCustomer_CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Booking_Customers_CompanionCustomer_CustomerId",
                table: "Booking");

            migrationBuilder.DropForeignKey(
                name: "FK_Booking_Customers_PrincipalCustomer_CustomerId",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "CompanionCustomer_BikePrice",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "CompanionCustomer_BikeType",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "PrincipalCustomer_BikePrice",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "PrincipalCustomer_BikeType",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "RoomType",
                table: "Booking");

            migrationBuilder.RenameColumn(
                name: "DoubleRoomSupplementPrice",
                table: "Tours",
                newName: "SingleRoomSupplementPrice");

            migrationBuilder.RenameColumn(
                name: "PrincipalCustomer_CustomerId",
                table: "Booking",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "RoomAdditionalCost",
                table: "Booking",
                newName: "TotalPrice");

            migrationBuilder.RenameColumn(
                name: "CompanionCustomer_CustomerId",
                table: "Booking",
                newName: "CompanionId");

            migrationBuilder.RenameIndex(
                name: "IX_Booking_PrincipalCustomer_CustomerId",
                table: "Booking",
                newName: "IX_Booking_CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_Booking_CompanionCustomer_CustomerId",
                table: "Booking",
                newName: "IX_Booking_CompanionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Customers_CompanionId",
                table: "Booking",
                column: "CompanionId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Customers_CustomerId",
                table: "Booking",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
