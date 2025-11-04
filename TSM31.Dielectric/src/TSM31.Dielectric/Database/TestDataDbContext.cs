using Microsoft.EntityFrameworkCore;
using TSM31.Dielectric.Database.Entities;

// ReSharper disable StringLiteralTypo
namespace TSM31.Dielectric.Database;

/// <summary>
/// Database context for accessing legacy SQL Server test data (Params and Xref tables).
/// This is a read-only connection to the existing TestData database.
/// </summary>
public class TestDataDbContext : DbContext
{
    public TestDataDbContext()
    {
    }

    public TestDataDbContext(DbContextOptions<TestDataDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Test parameters table containing specifications, limits, and requirements.
    /// </summary>
    public virtual DbSet<Param> Params { get; set; } = null!;

    /// <summary>
    /// Cross-reference table mapping serial numbers to catalog numbers and work orders.
    /// </summary>
    public virtual DbSet<Xref> Xrefs { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Default connection string - can be overridden via dependency injection
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                "Server=RAD-SQL;Database=TestData;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Param entity (no primary key)
        modelBuilder.Entity<Param>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.Arrestor).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.AuxiliaryDeviceMaterialFactor).HasMaxLength(6).IsUnicode(false);
            entity.Property(e => e.AuxiliaryDeviceWattsAt85).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.CatalogNumber).HasMaxLength(13).IsUnicode(false);
            entity.Property(e => e.CondLossAndIzRequired).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.CondMaxDesign).HasMaxLength(7).IsUnicode(false);
            entity.Property(e => e.CondMaxQuoted).HasMaxLength(7).IsUnicode(false);
            entity.Property(e => e.CondMinDesign).HasMaxLength(7).IsUnicode(false);
            entity.Property(e => e.CoreLossRequired).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.CoreMaxDesign).HasMaxLength(6).IsUnicode(false);
            entity.Property(e => e.CoreMaxQuoted).HasMaxLength(6).IsUnicode(false);
            entity.Property(e => e.CoreMinDesign).HasMaxLength(6).IsUnicode(false);
            entity.Property(e => e.DisconnectPresent).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.DoeconductorLossMultiplier).HasMaxLength(6).IsUnicode(false)
                .HasColumnName("DOEConductorLossMultiplier");
            entity.Property(e => e.DoecustomerLimit).HasMaxLength(8).IsUnicode(false)
                .HasColumnName("DOECustomerLimit");
            entity.Property(e => e.DoewattsLimit).HasMaxLength(8).IsUnicode(false)
                .HasColumnName("DOEWattsLimit");
            entity.Property(e => e.FirstInducedRequired).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.FourLvbhipotRequired).HasMaxLength(1).IsUnicode(false)
                .HasColumnName("FourLVBHipotRequired");
            entity.Property(e => e.FourLvbhipotTestTime).HasMaxLength(2).IsUnicode(false)
                .HasColumnName("FourLVBHipotTestTime");
            entity.Property(e => e.FourLvbhipotTestVoltage).HasMaxLength(5).IsUnicode(false)
                .HasColumnName("FourLVBHipotTestVoltage");
            entity.Property(e => e.H1ImpulseRequired).HasMaxLength(1).IsUnicode(false)
                .HasColumnName("H1ImpulseRequired");
            entity.Property(e => e.H2ImpulseRequired).HasMaxLength(1).IsUnicode(false)
                .HasColumnName("H2ImpulseRequired");
            entity.Property(e => e.H3ImpulseRequired).HasMaxLength(1).IsUnicode(false)
                .HasColumnName("H3ImpulseRequired");
            entity.Property(e => e.Hvhipotlimit).HasMaxLength(3).IsUnicode(false)
                .HasColumnName("HVHIPOTLimit");
            entity.Property(e => e.InducedVolts).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.InducedWattsLimit).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.Kva).HasMaxLength(6).IsUnicode(false).HasColumnName("KVA");
            entity.Property(e => e.Lvhipotlimit).HasMaxLength(3).IsUnicode(false)
                .HasColumnName("LVHIPOTLimit");
            entity.Property(e => e.PercentIexMaxDesign).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.PercentIexMaxQuoted).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.PercentIexRequired).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.PercentIzMaxDesign).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.PercentIzMaxQuoted).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.PercentIzMinDesign).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.PercentIzMinQuoted).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.Polarity).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.PriBil).HasMaxLength(3).IsUnicode(false).HasColumnName("PriBIL");
            entity.Property(e => e.PriBushings).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.PriCoilCfg).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.PriHipotrequired).HasMaxLength(1).IsUnicode(false)
                .HasColumnName("PriHIPOTRequired");
            entity.Property(e => e.PriMaterial).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.PriRatings).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.Pv).HasMaxLength(5).IsUnicode(false).HasColumnName("PV");
            entity.Property(e => e.RatioAndBalanceRequired).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.RefTemp).HasMaxLength(3).IsUnicode(false);
            entity.Property(e => e.SecBil).HasMaxLength(3).IsUnicode(false).HasColumnName("SecBIL");
            entity.Property(e => e.SecBushings).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.SecCoilCfg).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.SecHipotrequired).HasMaxLength(1).IsUnicode(false)
                .HasColumnName("SecHIPOTRequired");
            entity.Property(e => e.SecMaterial).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.SecRatings).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.SecTestVolts).HasMaxLength(4).IsUnicode(false);
            entity.Property(e => e.SecondInducedRequired).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.SecondInducedTestTime).HasMaxLength(2).IsUnicode(false);
            entity.Property(e => e.SideBySideFlag).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.StrayLoss).HasMaxLength(6).IsUnicode(false);
            entity.Property(e => e.Sv).HasMaxLength(4).IsUnicode(false).HasColumnName("SV");
            entity.Property(e => e.TestNumber).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.TotalDielectricTests).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.TotalLossTests).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.UnitType).HasMaxLength(1).IsUnicode(false);
            entity.Property(e => e.WorkOrder).HasMaxLength(5).IsUnicode(false);
            entity.Property(e => e.X1ImpulseRequired).HasMaxLength(1).IsUnicode(false)
                .HasColumnName("X1ImpulseRequired");
            entity.Property(e => e.X2ImpulseRequired).HasMaxLength(1).IsUnicode(false)
                .HasColumnName("X2ImpulseRequired");
            entity.Property(e => e.X3ImpulseRequired).HasMaxLength(1).IsUnicode(false)
                .HasColumnName("X3ImpulseRequired");
        });

        // Configure Xref entity (no primary key)
        modelBuilder.Entity<Xref>(entity =>
        {
            entity.HasNoKey().ToTable("Xref");

            entity.Property(e => e.Catno).HasMaxLength(13).IsUnicode(false).HasColumnName("CATNO");
            entity.Property(e => e.Serno).HasMaxLength(10).IsUnicode(false).HasColumnName("SERNO");
            entity.Property(e => e.Workorder).HasMaxLength(5).IsUnicode(false)
                .HasColumnName("WORKORDER");
        });
    }
}
