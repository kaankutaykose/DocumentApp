using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserAndAdminTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "User"); // Eğer tablo ismi farklıysa güncelleyin

            migrationBuilder.DropTable(
                name: "Admin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
