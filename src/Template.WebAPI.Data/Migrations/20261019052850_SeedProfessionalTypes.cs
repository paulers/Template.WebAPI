using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Template.WebAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedProfessionalTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Professionals_ProfessionalTypes_ProfessionalTypeId",
                table: "Professionals");

            migrationBuilder.InsertData(
                table: "ProfessionalTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("0e831d38-267d-461e-9dff-00718ff75e82"), "General contractor" },
                    { new Guid("20628bc2-8c45-4407-a964-c12d7f99f750"), "HVAC technician" },
                    { new Guid("78bc6516-4e77-412d-b1c8-37ed556c9ddc"), "Roofer" },
                    { new Guid("b3767919-1a56-4a8c-b083-8943a53841f3"), "Plumber" },
                    { new Guid("ce091f19-d24d-45bb-b1a0-0c557d5695df"), "Electrician" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Professionals_ProfessionalTypes_ProfessionalTypeId",
                table: "Professionals",
                column: "ProfessionalTypeId",
                principalTable: "ProfessionalTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep the restrictive FK until the seeded types have been safely removed.
            migrationBuilder.DeleteData(
                table: "ProfessionalTypes",
                keyColumn: "Id",
                keyValue: new Guid("0e831d38-267d-461e-9dff-00718ff75e82"));

            migrationBuilder.DeleteData(
                table: "ProfessionalTypes",
                keyColumn: "Id",
                keyValue: new Guid("20628bc2-8c45-4407-a964-c12d7f99f750"));

            migrationBuilder.DeleteData(
                table: "ProfessionalTypes",
                keyColumn: "Id",
                keyValue: new Guid("78bc6516-4e77-412d-b1c8-37ed556c9ddc"));

            migrationBuilder.DeleteData(
                table: "ProfessionalTypes",
                keyColumn: "Id",
                keyValue: new Guid("b3767919-1a56-4a8c-b083-8943a53841f3"));

            migrationBuilder.DeleteData(
                table: "ProfessionalTypes",
                keyColumn: "Id",
                keyValue: new Guid("ce091f19-d24d-45bb-b1a0-0c557d5695df"));

            migrationBuilder.DropForeignKey(
                name: "FK_Professionals_ProfessionalTypes_ProfessionalTypeId",
                table: "Professionals");

            migrationBuilder.AddForeignKey(
                name: "FK_Professionals_ProfessionalTypes_ProfessionalTypeId",
                table: "Professionals",
                column: "ProfessionalTypeId",
                principalTable: "ProfessionalTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
