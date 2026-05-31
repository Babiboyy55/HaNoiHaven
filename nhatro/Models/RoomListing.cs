using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace nhatro.Models
{
    // 1. SỬA CHỮ 'Text' THÀNH 'Title'
    public class RoomAmenity
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty; 
        public string Text { get; set; } = string.Empty;
    }

    public class RoomListing
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài đăng")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá phòng")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập diện tích (m2)")]
        public double Area { get; set; }

        public string? Description { get; set; }

        public string? Rules { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public string StatusBadge { get; set; } = "Bản nháp"; 

        public string OwnerId { get; set; } = string.Empty;

        // --- CÁC THUỘC TÍNH DÙNG RIÊNG CHO GIAO DIỆN ---
        
        // Danh sách các ảnh thực tế trong Database
        public List<RoomImage> RoomImages { get; set; } = new List<RoomImage>();
        
        [NotMapped]
        public double? Rating { get; set; } // 2. THÊM DẤU CỎ (HỎI CHẤM) ĐỂ CHO PHÉP NULL

        [NotMapped]
        public bool IsFavorite { get; set; }

        [NotMapped]
        public List<RoomAmenity> Amenities { get; set; } = new List<RoomAmenity>();
    }
}