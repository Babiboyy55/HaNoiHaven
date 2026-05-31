using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace nhatro.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        [Required]
        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung sự cố cần sửa chữa")]
        public string Description { get; set; } = string.Empty;

        // Trạng thái: "Chờ xử lý", "Đang xử lý", "Đã hoàn thành"
        public string Status { get; set; } = "Chờ xử lý";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string OwnerId { get; set; } = string.Empty;
    }
}
