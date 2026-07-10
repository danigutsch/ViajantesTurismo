using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViajantesTurismo.Branding.Infrastructure.Migrations;

[DbContext(typeof(BrandingDbContext))]
[Migration("20260710120000_InitialBranding")]
partial class InitialBranding
{
    /// <inheritdoc />
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.9")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("ViajantesTurismo.Branding.Infrastructure.BrandingSettingsRecord", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid");

                b.Property<string>("AccentColor")
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnType("character varying(7)");

                b.Property<string>("BackgroundColor")
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnType("character varying(7)");

                b.Property<string>("BodyFontFamily")
                    .IsRequired()
                    .HasMaxLength(120)
                    .HasColumnType("character varying(120)");

                b.Property<string>("BrandName")
                    .IsRequired()
                    .HasMaxLength(120)
                    .HasColumnType("character varying(120)");

                b.Property<string>("HeadingFontFamily")
                    .IsRequired()
                    .HasMaxLength(120)
                    .HasColumnType("character varying(120)");

                b.Property<string>("LogoUri")
                    .HasMaxLength(2048)
                    .HasColumnType("character varying(2048)");

                b.Property<string>("PrimaryColor")
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnType("character varying(7)");

                b.Property<string>("TextColor")
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasColumnType("character varying(7)");

                b.HasKey("Id");

                b.ToTable("BrandingSettings", (string)null);
            });
#pragma warning restore 612, 618
    }
}
