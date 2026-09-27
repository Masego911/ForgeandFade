using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeAndFade.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPriceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PriceAtBooking",
                table: "Bookings",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceAtBooking",
                table: "Bookings");
        }
    }
}
