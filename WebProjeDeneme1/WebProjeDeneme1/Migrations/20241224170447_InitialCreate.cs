using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebProjeDeneme1.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Konumlar",
                columns: table => new
                {
                    KonumId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KonumAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Konumlar", x => x.KonumId);
                });

            migrationBuilder.CreateTable(
                name: "Uyeler",
                columns: table => new
                {
                    UyeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AdminMi = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uyeler", x => x.UyeId);
                });

            migrationBuilder.CreateTable(
                name: "UzmanlikAlanlari",
                columns: table => new
                {
                    UzmanlikAlaniId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UzmanlikAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UzmanlikAlanlari", x => x.UzmanlikAlaniId);
                });

            migrationBuilder.CreateTable(
                name: "Salonlar",
                columns: table => new
                {
                    SalonId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KonumId = table.Column<int>(type: "integer", nullable: false),
                    BaslangicSaat = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BitisSaat = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salonlar", x => x.SalonId);
                    table.ForeignKey(
                        name: "FK_Salonlar_Konumlar_KonumId",
                        column: x => x.KonumId,
                        principalTable: "Konumlar",
                        principalColumn: "KonumId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YapilabilenIslemler",
                columns: table => new
                {
                    YapilabilenIslemlerId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UzmanlikAlaniId = table.Column<int>(type: "integer", nullable: false),
                    IslemAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IslemUcreti = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    IslemSuresi = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YapilabilenIslemler", x => x.YapilabilenIslemlerId);
                    table.ForeignKey(
                        name: "FK_YapilabilenIslemler_UzmanlikAlanlari_UzmanlikAlaniId",
                        column: x => x.UzmanlikAlaniId,
                        principalTable: "UzmanlikAlanlari",
                        principalColumn: "UzmanlikAlaniId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Personeller",
                columns: table => new
                {
                    PersonelId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalonId = table.Column<int>(type: "integer", nullable: false),
                    UzmanlikAlanlari = table.Column<string>(type: "text", nullable: false),
                    BaslangicSaat = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BitisSaat = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personeller", x => x.PersonelId);
                    table.ForeignKey(
                        name: "FK_Personeller_Salonlar_SalonId",
                        column: x => x.SalonId,
                        principalTable: "Salonlar",
                        principalColumn: "SalonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Randevular",
                columns: table => new
                {
                    RandevuId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalonId = table.Column<int>(type: "integer", nullable: false),
                    PersonelId = table.Column<int>(type: "integer", nullable: false),
                    UyeId = table.Column<int>(type: "integer", nullable: false),
                    IslemId = table.Column<int>(type: "integer", nullable: false),
                    Gun = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Saat = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Onaylandi = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Randevular", x => x.RandevuId);
                    table.ForeignKey(
                        name: "FK_Randevular_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "PersonelId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Randevular_Salonlar_SalonId",
                        column: x => x.SalonId,
                        principalTable: "Salonlar",
                        principalColumn: "SalonId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Randevular_Uyeler_UyeId",
                        column: x => x.UyeId,
                        principalTable: "Uyeler",
                        principalColumn: "UyeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Randevular_YapilabilenIslemler_IslemId",
                        column: x => x.IslemId,
                        principalTable: "YapilabilenIslemler",
                        principalColumn: "YapilabilenIslemlerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Personeller_SalonId",
                table: "Personeller",
                column: "SalonId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_IslemId",
                table: "Randevular",
                column: "IslemId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_PersonelId",
                table: "Randevular",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_SalonId",
                table: "Randevular",
                column: "SalonId");

            migrationBuilder.CreateIndex(
                name: "IX_Randevular_UyeId",
                table: "Randevular",
                column: "UyeId");

            migrationBuilder.CreateIndex(
                name: "IX_Salonlar_KonumId",
                table: "Salonlar",
                column: "KonumId");

            migrationBuilder.CreateIndex(
                name: "IX_YapilabilenIslemler_UzmanlikAlaniId",
                table: "YapilabilenIslemler",
                column: "UzmanlikAlaniId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Randevular");

            migrationBuilder.DropTable(
                name: "Personeller");

            migrationBuilder.DropTable(
                name: "Uyeler");

            migrationBuilder.DropTable(
                name: "YapilabilenIslemler");

            migrationBuilder.DropTable(
                name: "Salonlar");

            migrationBuilder.DropTable(
                name: "UzmanlikAlanlari");

            migrationBuilder.DropTable(
                name: "Konumlar");
        }
    }
}
