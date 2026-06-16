using System;
using System.Collections.Generic;
using System.Linq;
using Nhom1.Models;

namespace Nhom1.Services
{
    public class GeofenceService
    {
        // Hằng số bán kính Trái Đất (tính bằng mét)
        private const double EarthRadius = 6371000;

        /// <summary>
        /// Thuật toán Haversine: Tính khoảng cách đường chim bay giữa 2 tọa độ GPS (trả về mét)
        /// </summary>
        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            // Chuyển đổi vĩ độ sang radian để tính toán
            var rLat1 = ToRadians(lat1);
            var rLat2 = ToRadians(lat2);

            // Công thức Haversine lõi
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(rLat1) * Math.Cos(rLat2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadius * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        /// <summary>
        /// Hàm này nhận tọa độ của người dùng và tìm điểm POI gần nhất có thể phát âm thanh
        /// Dựa trên Mức ưu tiên (Priority) nếu có nhiều POI trùng nhau.
        /// </summary>
        public POI CheckTriggeredPOI(double userLat, double userLng, List<POI> allPOIs)
        {
            var triggeredPOIs = new List<POI>();

            // Lọc ra tất cả các điểm mà người dùng đã bước vào vùng bán kính (Radius)
            foreach (var poi in allPOIs)
            {
                double distance = CalculateDistance(userLat, userLng, poi.Lat, poi.Lng);
                
                // Nếu khoảng cách hiện tại NHỎ HƠN hoặc BẰNG bán kính quy định của điểm đó
                if (distance <= poi.Radius)
                {
                    triggeredPOIs.Add(poi);
                }
            }

            // Nếu không vào vùng nào, trả về null
            if (!triggeredPOIs.Any())
            {
                return null;
            }

            // Nếu vào nhiều vùng cùng lúc (ví dụ điểm dừng xe buýt Khánh Hội và Xóm Chiếu quá sát nhau), 
            // chọn điểm có mức ưu tiên cao nhất để phát.
            return triggeredPOIs.OrderByDescending(p => p.Priority).First();
        }
    }
}