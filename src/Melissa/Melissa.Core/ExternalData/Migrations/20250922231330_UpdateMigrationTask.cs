using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Melissa.Infraestructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMigrationTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Tasks");
        }
    }
}
