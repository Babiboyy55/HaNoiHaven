using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace nhatro.Models
{
    public class Contract
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng")]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        public string? TenantId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên khách thuê")]
        public string TenantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại khách thuê")]
        public string TenantPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

        [Required(ErrorMessage = "Vui lòng nhập giá thuê thực tế")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá thuê phải lớn hơn hoặc bằng 0")]
        public decimal RentPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiền đặt cọc")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền đặt cọc phải lớn hơn hoặc bằng 0")]
        public decimal DepositAmount { get; set; }

        // "Chờ ký", "Đang hoạt động", "Đã thanh lý"
        public string Status { get; set; } = "Chờ ký";

        public string ContractTerms { get; set; } = string.Empty;

        public string OwnerId { get; set; } = string.Empty;
    }
}
