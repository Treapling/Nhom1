using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Services;

var builder = WebApplication.CreateBuilder(args);

// Khai báo kết nối cơ sở dữ liệu SQLite sử dụng chuỗi kết nối từ appsettings.json
// Đã chuyển sang SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddScoped<GeofenceService>();

// Thêm các dịch vụ cần thiết cho Web API
builder.Services.AddControllers();

// Cấu hình mã khóa bảo mật JWT
var key = Encoding.ASCII.GetBytes("SGU_TourGuide_SecretKey_2026_Secure_Super_Long_Key");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build(); 

// Cấu hình pipeline xử lý HTTP request
if (app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); 
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ---- TỰ ĐỘNG TẠO TÀI KHOẢN MẪU NẾU DATABASE TRỐNG ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // 1. Tạo tài khoản Admin
    if (!db.Users.Any(u => u.Username == "admin"))
    {
        db.Users.Add(new Nhom1.Models.User { Username = "admin", PasswordHash = "admin123", Role = "Admin", ShopName = "System Admin", IsActive = true });
    }
    
    // 2. Tạo tài khoản Vendor và cấp sẵn 1 POI cho Vendor này
    if (!db.Users.Any(u => u.Username == "vendor1"))
    {
        var vendor = new Nhom1.Models.User { Username = "vendor1", PasswordHash = "vendor123", Role = "Vendor", ShopName = "Quán Ốc Vũ", IsActive = true };
        db.Users.Add(vendor);
        db.SaveChanges(); // Lưu để lấy ID của vendor

        // Tạo 1 địa điểm (POI) gắn liền với chủ quán này
        db.POIs.Add(new Nhom1.Models.POI { 
            Name = "Quán Ốc Vũ", 
            Description = "Quán ốc ngon nhất quận 4", 
            Lat = 10.7622, 
            Lng = 106.6825, 
            Radius = 50, 
            Priority = 1, 
            UserId = vendor.Id // GẮN CHỦ SỞ HỮU TẠI ĐÂY
        });
        db.SaveChanges();
    }
}
// ---------------------------------------------------------

app.Run();