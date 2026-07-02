using System;
using System.Collections.Generic;
using System.Linq;
using Nhom1.Models;

namespace Nhom1.Services
{
    /// <summary>
    /// [SERVICE] Dịch vụ Geofence - Tính toán khoảng cách địa lý và kiểm tra vùng kích hoạt
    /// Sử dụng công thức Haversine để tính khoảng cách giữa 2 tọa độ GPS
    /// </summary>
    public class GeofenceService
    {
        private const double EarthRadius = 6371000; // Bán kính Trái Đất (tính bằng mét)

        /// <summary>
        /// Tính khoảng cách giữa 2 điểm trên bề mặt Trái Đất bằng công thức Haversine
        /// Đầu vào: vĩ độ (lat) và kinh độ (lon) của 2 điểm (đơn vị: độ)
        /// Đầu ra: khoảng cách (đơn vị: mét)
        /// </summary>
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);   // Chênh lệch vĩ độ (radian)
            var dLon = ToRadians(lon2 - lon1);   // Chênh lệch kinh độ (radian)
            var rLat1 = ToRadians(lat1);         // Vĩ độ điểm 1 (radian)
            var rLat2 = ToRadians(lat2);         // Vĩ độ điểm 2 (radian)

            // Công thức Haversine:
            // a = sin²(Δlat/2) + cos(lat1)·cos(lat2)·sin²(Δlon/2)
            // c = 2 · atan2(√a, √(1-a))
            // d = R · c
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(rLat1) * Math.Cos(rLat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadius * c;
        }

        /// <summary>
        /// Chuyển đổi góc từ độ sang radian
        /// </summary>
        private double ToRadians(double angle) { return Math.PI * angle / 180.0; }

        /// <summary>
        /// Kiểm tra vị trí người dùng có nằm trong vùng bán kính (geofence) của POI nào không
        /// Đầu vào: tọa độ user + danh sách tất cả POI
        /// Đầu ra: danh sách POI bị kích hoạt, sắp xếp theo độ ưu tiên (Priority) giảm dần
        /// Nếu không có POI nào khớp => trả về mảng rỗng
        /// </summary>
        public List<POI> CheckTriggeredPOIs(double userLat, double userLng, List<POI> allPOIs)
        {
            var triggeredPOIs = new List<POI>();
            foreach (var poi in allPOIs)
            {
                // Tính khoảng cách từ user đến POI
                double distance = CalculateDistance(userLat, userLng, poi.Lat, poi.Lng);
                // Nếu khoảng cách <= bán kính (Radius) của POI => kích hoạt
                if (distance <= poi.Radius)
                {
                    triggeredPOIs.Add(poi);
                }
            }
            // Sắp xếp theo độ ưu tiên giảm dần (POI quan trọng nhất hiện đầu tiên)
            return triggeredPOIs.OrderByDescending(p => p.Priority).ToList();
        }
    }
}