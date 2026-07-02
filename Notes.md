# 🗺️ TOUR GUIDE APPLICATION - NHOM1

## 📋 TỔNG QUAN PROJECT

**Ứng dụng hướng dẫn du lịch thông minh** - Cho phép khách tham quan quét QR tại các địa điểm, nghe audio hướng dẫn đa ngôn ngữ, xem thực đơn, đánh giá. Hỗ trợ định vị Geofence để tự động kích hoạt nội dung khi đến gần địa điểm.

**Công nghệ**: ASP.NET Core Web API + Entity Framework Core + SQL Server + JWT Authentication + Swagger

---

## 🏗️ CẤU TRÚC FILE & CHỨC NĂNG

### 1. Program.cs — Khởi động ứng dụng

| Section | Chức năng |
|---------|-----------|
| **Services DI** | Đăng ký DbContext (SQL Server), CORS (AllowAll), GeofenceService (Scoped), Controllers, JWT Auth, Swagger |
| **Middleware Pipeline** | StaticFiles → CORS → Auth → Authorization → Controllers |
| **Seed Data** | Tự động tạo tài khoản `admin/admin123` (Admin) và `vendor1/vendor123` (Vendor) + 1 POI mẫu nếu database trống |

---

### 2. Models/ — Định nghĩa dữ liệu (8 bảng)

| File | Bảng | Chức năng chính |
|------|------|-----------------|
| **User.cs** | Users | Tài khoản: username, password, Role (Admin/Vendor/GuestFree/GuestPremium), ShopName, MaxPOISlots |
| **POI.cs** | POIs | Địa điểm: tên, mô tả (đa ngôn ngữ: vi/en/zh/ko/ja), tọa độ GPS (Lat/Lng), Radius (geofence), Priority, ApprovalStatus |
| **Audio.cs** | Audios | File âm thanh: đường dẫn file, ngôn ngữ, IsPremium (Free/Premium) |
| **Menu.cs** | Menus | Món ăn: tên, giá, mô tả, hình ảnh |
| **Review.cs** | Reviews | Đánh giá: số sao (1-5), bình luận |
| **Tour.cs** | Tours | Tour du lịch: chứa nhiều POI (quan hệ nhiều-nhiều) |
| **TrackingLog.cs** | TrackingLogs | Nhật ký: EventType (SCAN_QR/GPS_TRIGGER/VIEW_MENU), SessionToken |
| **VendorProfile.cs** | VendorProfiles | Hồ sơ Vendor: avatar, chứng nhận ATTP |

---

### 3. Data/AppDbContext.cs — Database Context

- **8 DbSet** tương ứng 8 bảng Models
- **OnModelCreating**: Cấu hình quan hệ nhiều-nhiều giữa Tour và POI

---

### 4. Controllers/ — API Endpoints (8 Controllers)

#### 📌 AuthController — Xác thực (`/api/auth`)
| Method | Endpoint | Quyền | Chức năng |
|--------|----------|-------|-----------|
| POST | `/login` | Public | Đăng nhập (username + password) → trả JWT token |
| POST | `/guest-free` | Public | Cấp token Free (hạn 1 năm) |
| POST | `/guest-premium` | Public | Cấp token Premium (hạn 24h) |

#### 📌 POIController — Địa điểm (`/api/poi`)
| Method | Endpoint | Quyền | Chức năng |
|--------|----------|-------|-----------|
| GET | `/admin` | Admin | Danh sách tất cả POI + lượt nghe + audio |
| GET | `/vendor` | Vendor | Danh sách POI của Vendor + rating |
| PUT | `/{id}` | Vendor | Cập nhật POI → chờ Admin duyệt lại |
| POST | `/` | Auth | Thêm POI mới (Vendor bị giới hạn slot) |
| GET | `/{id}` | Auth | Xem chi tiết POI + tự động ghi SCAN_QR |

#### 📌 UserController — Người dùng (`/api/user`)
| Method | Endpoint | Quyền | Chức năng |
|--------|----------|-------|-----------|
| POST | `/vendor` | Admin | Tạo tài khoản Vendor mới |
| POST | `/buy-slot` | Vendor | Mua thêm slot địa điểm (+1 MaxPOISlots) |

#### 📌 AnalyticsController — Thống kê (`/api/analytics`)
| Method | Endpoint | Quyền | Chức năng |
|--------|----------|-------|-----------|
| GET | `/kpi-summary` | Admin | Dashboard: online users, active/pending POIs, reviews, scans, POI hot nhất (Bayesian Average) |
| GET | `/chart?type=` | Admin | Biểu đồ: hour/ngày/tháng |
| GET | `/vendor-kpi` | Vendor | Thống kê riêng của Vendor |
| POST | `/logout-guest` | Auth | Xóa tracking logs khi logout |

#### 📌 LocationController — GPS Geofence (`/api/location`)
| Method | Endpoint | Quyền | Chức năng |
|--------|----------|-------|-----------|
| GET | `/check?lat=&lng=` | Auth | Kiểm tra vị trí có trong geofence POI không → ghi GPS_TRIGGER |

#### 📌 MenuController — Thực đơn (`/api/menu`)
| Method | Endpoint | Quyền | Chức năng |
|--------|----------|-------|-----------|
| GET | `/poi/{poiId}` | Auth | Xem thực đơn của POI |
| POST | `/` | Vendor | Thêm món ăn |
| PUT | `/{id}` | Vendor | Sửa món ăn |
| DELETE | `/{id}` | Vendor | Xóa món ăn |

#### 📌 ReviewController — Đánh giá (`/api/review`)
| Method | Endpoint | Quyền | Chức năng |
|--------|----------|-------|-----------|
| POST | `/` | GuestFree/Premium | Gửi đánh giá (Free bị xóa comment) |
| GET | `/poi/{poiId}` | GuestPremium | Xem bình luận |
| GET | `/manage` | Admin/Vendor | Quản lý đánh giá (Vendor chỉ xem của mình) |

#### 📌 AudioController — File âm thanh (`/api/audio`)
| Method | Endpoint | Quyền | Chức năng |
|--------|----------|-------|-----------|
| POST | `/` | Auth | Upload file audio (kiểm tra trùng lặp) |
| PUT | `/{id}` | Vendor | Cập nhật thông tin audio |
| GET | `/poi/{poiId}` | Anonymous | Lấy danh sách audio |
| DELETE | `/{id}` | Auth | Xóa audio (cả file vật lý) |

---

### 5. Services/GeofenceService.cs — Logic Geofence

| Method | Chức năng |
|--------|-----------|
| `CalculateDistance()` | Tính khoảng cách 2 tọa độ bằng công thức **Haversine** (đơn vị: mét) |
| `CheckTriggeredPOIs()` | Kiểm tra user có trong vùng geofence POI không → trả về danh sách sắp xếp theo Priority |

---

### 6. DT0s/PoiDTO.cs — Data Transfer Object

Đóng gói dữ liệu POI (Id, Name, Description, Lat, Lng, Radius, AudioUrl) để truyền qua API, tránh expose entity gốc.

---

## 🔐 PHÂN QUYỀN (ROLES)

| Role | Mô tả | Quyền hạn |
|------|-------|-----------|
| **Admin** | Quản trị viên | Toàn quyền: xem tất cả POI, duyệt địa điểm, tạo Vendor, xem thống kê |
| **Vendor** | Chủ quán | Quản lý POI của mình, menu, audio, xem KPI, mua slot. Bị giới hạn bởi MaxPOISlots |
| **GuestFree** | Khách Free | Xem POI, nghe audio thường, đánh giá sao (không comment), quét QR |
| **GuestPremium** | Khách VIP 24h | Như Free + nghe audio Premium, xem bình luận, hạn 24h |

---

## 📊 THUẬT TOÁN NỔI BẬT

### Bayesian Average Ranking (Xếp hạng POI thông minh)

```
C = Điểm trung bình toàn hệ thống
m = Ngưỡng Bayes (số review trung bình 1 POI)
R = Điểm trung bình của từng POI
v = Số lượng review của POI đó

BayesianRating = (v/(v+m))*R + (m/(v+m))*C
FinalScore     = BayesianRating * 0.6 + Log(lượt nghe + 1) * 0.4
```

**Mục đích**: Chống gian lận điểm - POI mới có ít review không bị điểm quá cao hoặc quá thấp.

---

## ⚙️ CẤU HÌNH

### appsettings.json
- **ConnectionStrings.DefaultConnection**: Chuỗi kết nối SQL Server (Server local, DB: `VinhKhanhTourGuideDB`)
- **Logging**: Cấu hình log level
- **AllowedHosts**: Cho phép tất cả host (`*`)

### JWT Secret Key
`SGU_TourGuide_SecretKey_2026_Secure_Super_Long_Key`

---

## 🌐 FRONTEND (wwwroot/)

| File | Chức năng |
|------|-----------|
| `index.html` | Trang chủ khách tham quan (quét QR, xem POI, nghe audio) |
| `admin.html` | Dashboard Admin (quản lý POI, duyệt địa điểm, thống kê) |
| `vendor.html` | Dashboard Vendor (quản lý POI, menu, audio, KPI) |
| `checkout.html` | Thanh toán mua slot (khách vãng lai) |
| `vendor-checkout.html` | Thanh toán mua slot (Vendor) |
| `audios/` | Thư mục chứa file .mp3, .m4a audio cho từng POI |

---

## 📌 TÀI KHOẢN MẪU (SEED DATA)

| Tài khoản | Mật khẩu | Role | ShopName |
|-----------|----------|------|----------|
| `admin` | `admin123` | Admin | System Admin |
| `vendor1` | `vendor123` | Vendor | Quán Ốc Vũ |