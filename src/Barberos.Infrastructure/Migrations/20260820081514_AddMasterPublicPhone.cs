using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barberos.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterPublicPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicPhone",
                table: "Masters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicPhone",
                table: "Masters");
        }
    }
}
