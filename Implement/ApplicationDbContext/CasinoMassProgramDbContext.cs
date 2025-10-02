using Implement.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Implement.ApplicationDbContext;

public class CasinoMassProgramDbContext : DbContext
{
    public CasinoMassProgramDbContext(DbContextOptions<CasinoMassProgramDbContext> options) : base(options) { }

    public DbSet<ImportBatch> ImportBatches { get; set; }
    public DbSet<ImportRow> ImportRows { get; set; }
    public DbSet<ImportCellError> ImportCellErrors { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<AwardSettlement> AwardSettlements { get; set; }
    public DbSet<TeamRepresentative> TeamRepresentatives { get; set; }
    public DbSet<TeamRepresentativeMember> TeamRepresentativeMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Import tracking
        modelBuilder.Entity<ImportBatch>()
            .HasMany(b => b.Rows)
            .WithOne(r => r.Batch!)
            .HasForeignKey(r => r.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ImportRow>()
            .HasMany(r => r.Errors)
            .WithOne(e => e.Row!)
            .HasForeignKey(e => e.RowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Member uniqueness
        modelBuilder.Entity<Member>()
            .HasIndex(m => m.MemberCode)
            .IsUnique();

        // TeamRepresentative uniqueness by external ID
        modelBuilder.Entity<TeamRepresentative>()
            .HasIndex(tr => tr.ExternalId)
            .IsUnique();

        // Join table composite PK and relationships
        modelBuilder.Entity<TeamRepresentativeMember>()
            .HasKey(x => new { x.TeamRepresentativeId, x.MemberId });

        modelBuilder.Entity<TeamRepresentativeMember>()
            .HasOne(x => x.TeamRepresentative)
            .WithMany(tr => tr.Members)
            .HasForeignKey(x => x.TeamRepresentativeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeamRepresentativeMember>()
            .HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // AwardSettlement monetary precision
        modelBuilder.Entity<AwardSettlement>()
            .Property(a => a.CasinoWinLoss)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<AwardSettlement>()
            .Property(a => a.AwardSettlementAmount)
            .HasColumnType("decimal(18,2)");
    }
}