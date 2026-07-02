using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nhom1.Data;
using Nhom1.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CẤU HÌNH DỊCH VỤ (SERVICES) - ĐĂNG KÝ CÁC THÀNH PHẦN VÀO HỆ THỐNG DI (Dependency Injection)
// ============================================================

// [KẾT NỐI CSDL] Đăng ký DbContext với SQL Server, đọc chuỗi kết nối từ appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// [CORS] Cho phép tất cả các nguồn gốc (origin), phương thức (method) và header truy cập API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// [GEOFENCE SERVICE] Đăng ký dịch vụ tính toán vùng địa lý (dạng Scoped: tạo mới mỗi request)
builder.Services.AddScoped<GeofenceService>();

// [CONTROLLERS] Kích hoạt các Controller API
builder.Services.AddControllers();

// [JWT AUTHENTICATION] Cấu hình xác thực bằng token JWT
// Secret key dùng để ký và xác thực token
var key = Encoding.ASCII.GetBytes("SGU_TourGuide_SecretKey_2026_Secure_Super_Long_Key");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; // Không yêu cầu HTTPS (dành cho môi trường dev)
    x.SaveToken = true;             // Lưu token để xử lý sau
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,                 // Kiểm tra chữ ký token
        IssuerSigningKey = new SymmetricSecurityKey(key), // Khóa đối xứng để giải mã token
        ValidateIssuer = false,  // Không kiểm tra issuer (người phát hành)
        ValidateAudience = false // Không kiểm tra audience (đối tượng nhận)
    };
});

// [SWAGGER] Cấu hình Swagger để tạo tài liệu API tự động
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ============================================================
// CẤU HÌNH PIPELINE XỬ LÝ HTTP REQUEST (MIDDLEWARE)
// Thứ tự thực thi: StaticFiles -> CORS -> Auth -> Authorization -> Controllers
// ============================================================

// [DEVELOPMENT MODE] Chỉ bật Swagger và file mặc định trong môi trường phát triển
if (app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();     // Cho phép truy cập file tĩnh trong thư mục wwwroot
app.UseCors("AllowAll");  // Áp dụng chính sách CORS

app.UseAuthentication();  // Xác thực người dùng qua JWT token
app.UseAuthorization();   // Phân quyền dựa trên Role

app.MapControllers();     // Ánh xạ các route từ Controller

// ============================================================
// SEED DATA TỰ ĐỘNG - TẠO TÀI KHOẢN MẪU KHI DATABASE TRỐNG
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // 1. Tạo tài khoản Admin mặc định (nếu chưa tồn tại)
    if (!db.Users.Any(u => u.Username == "admin"))
    {
        db.Users.Add(new Nhom1.Models.User { Username = "admin", PasswordHash = "admin123", Role = "Admin", ShopName = "System Admin", IsActive = true });
    }

    // 2. Tạo tài khoản Vendor mẫu + 1 POI mẫu (nếu chưa tồn tại)
    if (!db.Users.Any(u => u.Username == "vendor1"))
    {
        var vendor = new Nhom1.Models.User { Username = "vendor1", PasswordHash = "vendor123", Role = "Vendor", ShopName = "Quán Ốc Vũ", IsActive = true };
        db.Users.Add(vendor);
        db.SaveChanges(); // Lưu vendor trước để lấy ID

        // Tạo 1 địa điểm (POI) gắn với vendor này
        db.POIs.Add(new Nhom1.Models.POI { 
            Name = "Quán Ốc Vũ", 
            Description = "Quán ốc ngon nhất quận 4", 
            Lat = 10.7622, 
            Lng = 106.6825, 
            Radius = 50, 
            Priority = 1, 
            UserId = vendor.Id // Gán quyền sở hữu cho vendor
        });
        db.SaveChanges();
    }
}

// ============================================================
// KHỞI CHẠY ỨNG DỤNG
// ============================================================
app.Run();