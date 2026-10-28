using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Admin.Infrastructure.Migrations;

/// <inheritdoc />
public partial class UseCivilDates : Migration
{
    /// <inheritdoc />
    /// <remarks>Preserves the UTC calendar date regardless of the database session timezone.</remarks>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Tours"
                ALTER COLUMN "StartDate" TYPE date USING ("StartDate" AT TIME ZONE 'UTC')::date,
                ALTER COLUMN "EndDate" TYPE date USING ("EndDate" AT TIME ZONE 'UTC')::date;
            ALTER TABLE "CustomerPersonalInfo"
                ALTER COLUMN "BirthDate" TYPE date USING ("BirthDate" AT TIME ZONE 'UTC')::date;
            """);
    }

    /// <inheritdoc />
    /// <remarks>Civil dates recover as UTC midnight. Original times of day cannot be reconstructed.</remarks>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Tours"
                ALTER COLUMN "StartDate" TYPE timestamp with time zone USING "StartDate"::timestamp AT TIME ZONE 'UTC',
                ALTER COLUMN "EndDate" TYPE timestamp with time zone USING "EndDate"::timestamp AT TIME ZONE 'UTC';
            ALTER TABLE "CustomerPersonalInfo"
                ALTER COLUMN "BirthDate" TYPE timestamp with time zone USING "BirthDate"::timestamp AT TIME ZONE 'UTC';
            """);
    }
}
