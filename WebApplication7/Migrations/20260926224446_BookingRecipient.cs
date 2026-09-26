using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForgeAndFade.Api.Migrations
{
    /// <inheritdoc />
    public partial class BookingRecipient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsForChild",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsForChild",
                table: "Bookings");
        }
    }
}
