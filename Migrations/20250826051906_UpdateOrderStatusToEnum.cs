using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Notifico.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderStatusToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Orders""
                ALTER COLUMN ""Status"" TYPE integer
                USING
                  CASE
                    WHEN ""Status"" = 'Beklemede' THEN 0
                    WHEN ""Status"" = 'Onaylandı' THEN 1
                    WHEN ""Status"" = 'KargoyaVerildi' THEN 2
                    WHEN ""Status"" = 'TeslimEdildi' THEN 3
                    WHEN ""Status"" = 'IptalEdildi' THEN 4
                    ELSE 0
                  END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
