using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace nhatro.Models
{
    public class RentalRequest
    {
        public int Id { get; set; }

        // Liên kết bài đăng phòng trọ mà Tenant muốn xem
        [Required]
        public int RoomListingId { get; set; }

        [ForeignKey("RoomListingId")]
        public RoomListing? RoomListing { get; set; }

        // Id của người gửi yêu cầu (Tenant)
        [Required]
        public string TenantId { get; set; } = string.Empty;

        // Tên + SĐT để chủ nhà liên lạc (lấy từ form trực tiếp)
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        public string TenantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string TenantPhone { get; set; } = string.Empty;

        // Ngày và khung giờ hẹn xem phòng
        [Required(ErrorMessage = "Vui lòng chọn ngày muốn xem phòng")]
        public DateTime PreferredDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khung giờ")]
        public string PreferredTime { get; set; } = string.Empty;

        // Lời nhắn gửi cho chủ nhà
        public string Message { get; set; } = string.Empty;

        // Trạng thái xử lý: "Chờ duyệt", "Đã duyệt", "Đã từ chối", "Đã hủy"
        public string Status { get; set; } = "Chờ duyệt";

        // Thời gian gửi yêu cầu
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
