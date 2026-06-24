using Microsoft.EntityFrameworkCore;
using Nhom1.Models;

namespace Nhom1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<POI> POIs { get; set; }
        public DbSet<Audio> Audios { get; set; } // BỔ SUNG LẠI DÒNG NÀY
        public DbSet<User> Users { get; set; } 
        public DbSet<TrackingLog> TrackingLogs { get; set; } 
        public DbSet<Tour> Tours { get; set; } // Bổ sung thêm để khớp với OnModelCreating dưới đây

        public DbSet<Review> Reviews { get; set; }

        public DbSet<Menu> Menus { get; set; }
        public DbSet<VendorProfile> VendorProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Tour>()
                .HasMany(t => t.POIs)
                .WithMany(p => p.Tours);
        }
    }
}