using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UserService.Infrastructure.Data;

#nullable disable

namespace UserService.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260815120000_AddPatientEsiaAddresses")]
    public partial class AddPatientEsiaAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in AddressColumns)
            {
                migrationBuilder.Sql(
                    $"""ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "{column}" text;""");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var column in AddressColumns)
                migrationBuilder.DropColumn(name: column, table: "PatientProfiles");
        }

        private static readonly string[] AddressColumns =
        [
            "ResidenceAddress_PostCode", "ResidenceAddress_Country", "ResidenceAddress_Region",
            "ResidenceAddress_City", "ResidenceAddress_Area", "ResidenceAddress_Street",
            "ResidenceAddress_House", "ResidenceAddress_Flat",
            "RegistrationAddress_PostCode", "RegistrationAddress_Country", "RegistrationAddress_Region",
            "RegistrationAddress_City", "RegistrationAddress_Area", "RegistrationAddress_Street",
            "RegistrationAddress_House", "RegistrationAddress_Flat"
        ];
    }
}
