using Microsoft.EntityFrameworkCore;
using Nhom1.Models;

namespace Nhom1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<POI> POIs { get; set; }
        public DbSet<Audio> Audios { get; set; }
        public DbSet<Tour> Tours { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Tour>()
                .HasMany(t => t.POIs)
                .WithMany(p => p.Tours);
        }
    }
}