using System.ComponentModel.DataAnnotations;

namespace nhatro.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên/số phòng")]
        public string RoomName { get; set; } = string.Empty; // VD: Phòng 101, Tầng 2...

        public string PropertyType { get; set; } = "Phòng trọ"; // Căn hộ, Nhà nguyên căn...

        [Required(ErrorMessage = "Vui lòng nhập giá thu thực tế")]
        public decimal RentPrice { get; set; }

        // Trạng thái: "Trống", "Đang thuê", "Bảo trì"
        public string Status { get; set; } = "Trống"; 

        // Thông tin người đang thuê (nếu có)
        public string? TenantName { get; set; }
        public string? TenantPhone { get; set; }

        // Liên kết với Chủ trọ
        public string OwnerId { get; set; } = string.Empty;
    }
}