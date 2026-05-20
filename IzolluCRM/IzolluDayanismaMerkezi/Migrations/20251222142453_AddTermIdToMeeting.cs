using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IzolluVakfi.Migrations
{
    /// <inheritdoc />
    public partial class AddTermIdToMeeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TermId",
                table: "Meetings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_TermId",
                table: "Meetings",
                column: "TermId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_Terms_TermId",
                table: "Meetings",
                column: "TermId",
                principalTable: "Terms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meetings_Terms_TermId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_TermId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "TermId",
                table: "Meetings");
        }
    }
}
