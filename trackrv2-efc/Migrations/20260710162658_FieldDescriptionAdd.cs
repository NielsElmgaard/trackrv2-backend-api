using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace trackrv2_efc.Migrations
{
    /// <inheritdoc />
    public partial class FieldDescriptionAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FieldDefinitions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "FieldDefinitions");
        }
    }
}
