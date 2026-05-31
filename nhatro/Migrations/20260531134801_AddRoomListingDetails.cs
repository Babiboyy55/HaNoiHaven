using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace nhatro.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomListingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RoomListings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rules",
                table: "RoomListings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "RoomListings");

            migrationBuilder.DropColumn(
                name: "Rules",
                table: "RoomListings");
        }
    }
}
