using Microsoft.EntityFrameworkCore;

namespace UserService.Infrastructure.Data;

public static class PatientAddressSchema
{
    private const string EnsureColumnsSql =
        """
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "ResidenceAddress_PostCode" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "ResidenceAddress_Country" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "ResidenceAddress_Region" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "ResidenceAddress_City" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "ResidenceAddress_Area" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "ResidenceAddress_Street" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "ResidenceAddress_House" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "ResidenceAddress_Flat" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "RegistrationAddress_PostCode" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "RegistrationAddress_Country" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "RegistrationAddress_Region" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "RegistrationAddress_City" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "RegistrationAddress_Area" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "RegistrationAddress_Street" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "RegistrationAddress_House" text;
        ALTER TABLE "PatientProfiles" ADD COLUMN IF NOT EXISTS "RegistrationAddress_Flat" text;
        """;

    public static void EnsureColumns(AppDbContext db) =>
        db.Database.ExecuteSqlRaw(EnsureColumnsSql);
}
