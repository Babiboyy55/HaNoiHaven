using System;
using System.ComponentModel.DataAnnotations;

namespace nhatro.Models
{
    public class Review
    {
        public int Id { get; set; }

        // Đánh giá thuộc về bài đăng nào?
        public int RoomListingId { get; set; }
        public RoomListing? RoomListing { get; set; }

        // Ai là người đánh giá? (Liên kết với ApplicationUser)
        public string TenantId { get; set; } = string.Empty;
        public ApplicationUser? Tenant { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } // Số sao (1-5)

        public string? Comment { get; set; } // Nội dung nhận xét

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}