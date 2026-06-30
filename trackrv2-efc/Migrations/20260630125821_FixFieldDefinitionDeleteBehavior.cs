using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trackrv2_efc.Migrations
{
    /// <inheritdoc />
    public partial class FixFieldDefinitionDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntryValues_FieldDefinitions_FieldDefinitionId",
                table: "EntryValues");

            migrationBuilder.AddForeignKey(
                name: "FK_EntryValues_FieldDefinitions_FieldDefinitionId",
                table: "EntryValues",
                column: "FieldDefinitionId",
                principalTable: "FieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntryValues_FieldDefinitions_FieldDefinitionId",
                table: "EntryValues");

            migrationBuilder.AddForeignKey(
                name: "FK_EntryValues_FieldDefinitions_FieldDefinitionId",
                table: "EntryValues",
                column: "FieldDefinitionId",
                principalTable: "FieldDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
