using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeatSync.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BookingRequestId",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingRequestId",
                table: "Bookings",
                column: "BookingRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingRequestId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingRequestId",
                table: "Bookings");
        }
    }
}
