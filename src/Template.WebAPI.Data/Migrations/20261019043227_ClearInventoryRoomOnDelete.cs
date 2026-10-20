using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Template.WebAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClearInventoryRoomOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Rooms_RoomId",
                table: "InventoryItems");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Rooms_RoomId",
                table: "InventoryItems",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Rooms_RoomId",
                table: "InventoryItems");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Rooms_RoomId",
                table: "InventoryItems",
                column: "RoomId",
                principalTable: "Rooms",
                principalColumn: "Id");
        }
    }
}
