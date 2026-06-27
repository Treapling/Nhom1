using System;

namespace Nhom1.Models
{
    public class VisitorLog
    {
        public int Id { get; set; }
        
        // Định danh phiên người dùng (giữ không đổi trong 1 phiên duyệt web)
        public string SessionId { get; set; }
        
        // Thời gian ghi nhận
        public DateTime Timestamp { get; set; }
    }
}
