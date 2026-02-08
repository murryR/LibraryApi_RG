using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminUserType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ApplicationUsers SET UserType = 'Admin' WHERE Login = 'admin1';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ApplicationUsers SET UserType = 'User' WHERE Login = 'admin1';");
        }
    }
}
