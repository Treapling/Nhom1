namespace Nhom1.DTOs
{
    /// <summary>
    /// [DTO] Data Transfer Object cho POI - Đóng gói dữ liệu địa điểm khi truyền qua API
    /// Dùng để tránh expose toàn bộ entity POI ra ngoài (bảo mật hơn)
    /// </summary>
    public class PoiDTO
    {
        public int Id { get; set; }              // ID địa điểm
        public string Name { get; set; }         // Tên địa điểm
        public string Description { get; set; }  // Mô tả địa điểm
        public double Latitude { get; set; }     // Vĩ độ (Latitude)
        public double Longitude { get; set; }    // Kinh độ (Longitude)
        public double Radius { get; set; }       // Bán kính vùng geofence (mét)
        public string AudioUrl { get; set; }     // Đường dẫn file audio (hiện không còn sử dụng)
    }
}