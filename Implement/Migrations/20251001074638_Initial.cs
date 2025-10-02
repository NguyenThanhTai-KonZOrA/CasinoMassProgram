using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Implement.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ValidRows = table.Column<int>(type: "int", nullable: false),
                    InvalidRows = table.Column<int>(type: "int", nullable: false),
                    FileContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamRepresentatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamRepresentatives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportRows_ImportBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AwardSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamRepresentativeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonthStart = table.Column<DateOnly>(type: "date", nullable: false),
                    SettlementDoc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    No = table.Column<int>(type: "int", nullable: false),
                    JoinedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastGamingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Eligible = table.Column<bool>(type: "bit", nullable: false),
                    CasinoWinLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AwardSettlementAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwardSettlements_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AwardSettlements_TeamRepresentatives_TeamRepresentativeId",
                        column: x => x.TeamRepresentativeId,
                        principalTable: "TeamRepresentatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamRepresentativeMembers",
                columns: table => new
                {
                    TeamRepresentativeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamRepresentativeMembers", x => new { x.TeamRepresentativeId, x.MemberId });
                    table.ForeignKey(
                        name: "FK_TeamRepresentativeMembers_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamRepresentativeMembers_TeamRepresentatives_TeamRepresentativeId",
                        column: x => x.TeamRepresentativeId,
                        principalTable: "TeamRepresentatives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportCellErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Column = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportCellErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportCellErrors_ImportRows_RowId",
                        column: x => x.RowId,
                        principalTable: "ImportRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AwardSettlements_MemberId",
                table: "AwardSettlements",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardSettlements_TeamRepresentativeId",
                table: "AwardSettlements",
                column: "TeamRepresentativeId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportCellErrors_RowId",
                table: "ImportCellErrors",
                column: "RowId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRows_BatchId",
                table: "ImportRows",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_MemberCode",
                table: "Members",
                column: "MemberCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamRepresentativeMembers_MemberId",
                table: "TeamRepresentativeMembers",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamRepresentatives_ExternalId",
                table: "TeamRepresentatives",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AwardSettlements");

            migrationBuilder.DropTable(
                name: "ImportCellErrors");

            migrationBuilder.DropTable(
                name: "TeamRepresentativeMembers");

            migrationBuilder.DropTable(
                name: "ImportRows");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "TeamRepresentatives");

            migrationBuilder.DropTable(
                name: "ImportBatches");
        }
    }
}
