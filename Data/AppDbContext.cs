using Microsoft.EntityFrameworkCore;
using Nhom1.Models;

namespace Nhom1.Data
{
    /// <summary>
    /// [DATA] DbContext - Cầu nối giữa ứng dụng và cơ sở dữ liệu SQL Server
    /// Định nghĩa các bảng (DbSet) và cấu hình quan hệ giữa các bảng
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Định nghĩa các bảng trong database
        public DbSet<POI> POIs { get; set; }                    // Bảng địa điểm (Points of Interest)
        public DbSet<Audio> Audios { get; set; }                // Bảng file âm thanh
        public DbSet<User> Users { get; set; }                  // Bảng người dùng
        public DbSet<TrackingLog> TrackingLogs { get; set; }    // Bảng nhật ký tương tác
        public DbSet<Tour> Tours { get; set; }                  // Bảng tour du lịch
        public DbSet<Review> Reviews { get; set; }              // Bảng đánh giá
        public DbSet<Menu> Menus { get; set; }                  // Bảng thực đơn / món ăn
        public DbSet<VendorProfile> VendorProfiles { get; set; } // Bảng hồ sơ Vendor

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ nhiều-nhiều giữa Tour và POI
            // 1 Tour có nhiều POI, 1 POI có thể thuộc nhiều Tour
            modelBuilder.Entity<Tour>()
                .HasMany(t => t.POIs)
                .WithMany(p => p.Tours);
        }
    }
}