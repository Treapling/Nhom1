using System;
using System.Collections.Generic;
using System.Linq;
using Nhom1.Models;

namespace Nhom1.Services
{
    public class GeofenceService
    {
        private const double EarthRadius = 6371000;

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var rLat1 = ToRadians(lat1);
            var rLat2 = ToRadians(lat2);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(rLat1) * Math.Cos(rLat2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadius * c;
        }

        private double ToRadians(double angle) { return Math.PI * angle / 180.0; }

        // ĐÃ SỬA: Trả về Danh sách (List) thay vì 1 POI duy nhất
        public List<POI> CheckTriggeredPOIs(double userLat, double userLng, List<POI> allPOIs)
        {
            var triggeredPOIs = new List<POI>();
            foreach (var poi in allPOIs)
            {
                double distance = CalculateDistance(userLat, userLng, poi.Lat, poi.Lng);
                if (distance <= poi.Radius)
                {
                    triggeredPOIs.Add(poi);
                }
            }
            // Trả về mảng rỗng nếu không lọt vào vùng nào. Nếu có, sắp xếp theo độ ưu tiên giảm dần.
            return triggeredPOIs.OrderByDescending(p => p.Priority).ToList();
        }
    }
}