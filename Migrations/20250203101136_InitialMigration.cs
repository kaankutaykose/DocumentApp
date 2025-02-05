using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Topic",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Unit",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unit", x => x.UnitId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Topic_UnitId",
                table: "Topic",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_UnitId",
                table: "Documents",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Unit_UnitId",
                table: "Documents",
                column: "UnitId",
                principalTable: "Unit",
                principalColumn: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Topic_Unit_UnitId",
                table: "Topic",
                column: "UnitId",
                principalTable: "Unit",
                principalColumn: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Unit_UnitId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Topic_Unit_UnitId",
                table: "Topic");

            migrationBuilder.DropTable(
                name: "Unit");

            migrationBuilder.DropIndex(
                name: "IX_Topic_UnitId",
                table: "Topic");

            migrationBuilder.DropIndex(
                name: "IX_Documents_UnitId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Topic");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Documents");
        }
    }
}
