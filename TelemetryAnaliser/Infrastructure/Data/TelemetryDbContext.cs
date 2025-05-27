using Microsoft.EntityFrameworkCore;
using TelemetryAnalyzer.Core.Models;

namespace TelemetryAnalyzer.Infrastructure.Data
{
    public class TelemetryDbContext : DbContext
    {
        public DbSet<TelemetrySession> Sessions { get; set; }
        public DbSet<TelemetryDataPoint> DataPoints { get; set; }

        public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TelemetrySession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Source).HasMaxLength(500);
                entity.Property(e => e.FilePath).HasMaxLength(1000);
                entity.HasMany(e => e.DataPoints)
                      .WithOne(e => e.Session)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TelemetryDataPoint>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.JsonData).IsRequired();
                entity.HasIndex(e => e.Timestamp);
            });
        }
    }
}