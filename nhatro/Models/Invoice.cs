using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace nhatro.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [Required]
        public string TenantName { get; set; } = string.Empty;

        [Required]
        [Range(1, 12)]
        public int BillingMonth { get; set; }

        [Required]
        public int BillingYear { get; set; }

        [Required]
        public decimal RoomPrice { get; set; }

        public double ElectricityOld { get; set; }
        public double ElectricityNew { get; set; }
        public decimal ElectricityPrice { get; set; } = 3500; // Mặc định 3500 VND/kWh

        public double WaterOld { get; set; }
        public double WaterNew { get; set; }
        public decimal WaterPrice { get; set; } = 20000; // Mặc định 20000 VND/m3

        public decimal ServiceFees { get; set; } // Phí dịch vụ khác (rác, mạng, vệ sinh...)

        public decimal TotalAmount { get; set; }

        // "Chưa thanh toán", "Đã thanh toán"
        public string Status { get; set; } = "Chưa thanh toán";

        public DateTime? PaymentDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
