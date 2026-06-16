using Microsoft.EntityFrameworkCore;
using Nhom1.Data;

var builder = WebApplication.CreateBuilder(args);

// Khai báo kết nối cơ sở dữ liệu SQLite sử dụng chuỗi kết nối từ appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Thêm các dịch vụ cần thiết cho Web API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Cấu hình pipeline xử lý HTTP request (bao gồm Swagger để kiểm thử API)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();