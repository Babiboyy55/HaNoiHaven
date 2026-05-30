using System.Collections.Generic;
using System.Threading.Tasks;
using nhatro.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

namespace nhatro.Services
{
    public class RoomService : IRoomService
    {
        private readonly AppDbContext _context;

        public RoomService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoomListing>> GetFeaturedRoomsAsync(
            string? query = null,
            long? minPrice = null,
            long? maxPrice = null,
            string[]? roomTypes = null,
            string[]? amenities = null,
            string? distance = null)
        {
            // 1. Truy vấn các bài đăng thực tế từ Database, kèm danh sách ảnh của bài đăng đó
            var dbQuery = _context.RoomListings
                                  .Include(r => r.RoomImages)
                                  .AsQueryable();

            // Chỉ hiển thị các bài viết đang ở trạng thái hoạt động ("Đã đăng") hoặc không thiết lập
            dbQuery = dbQuery.Where(r => r.StatusBadge == "Đã đăng" || string.IsNullOrEmpty(r.StatusBadge));

            // 2. Tìm kiếm theo tiêu đề hoặc vị trí (khu vực)
            if (!string.IsNullOrWhiteSpace(query))
            {
                dbQuery = dbQuery.Where(r => 
                    r.Location.Contains(query) || 
                    r.Title.Contains(query)
                );
            }

            // 3. Lọc theo giá
            if (minPrice.HasValue)
            {
                dbQuery = dbQuery.Where(r => r.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                dbQuery = dbQuery.Where(r => r.Price <= maxPrice.Value);
            }

            // Chạy truy vấn kéo dữ liệu về RAM
            var listings = await dbQuery.ToListAsync();

            // 4. Thiết lập metadata động (Rating, Amenities) cho từng bài viết để phục vụ UI
            foreach (var room in listings)
            {
                PopulateListingMetadata(room);
            }

            // 5. Lọc thêm trong RAM đối với các bộ lọc phức tạp (Loại phòng & Tiện nghi)
            var result = listings.AsEnumerable();

            if (roomTypes != null && roomTypes.Any())
            {
                result = result.Where(r => roomTypes.Any(rt => 
                    (!string.IsNullOrEmpty(r.Title) && r.Title.Contains(rt, StringComparison.OrdinalIgnoreCase)) || 
                    (r.Amenities != null && r.Amenities.Any(a => 
                        (a.Title != null && a.Title.Contains(rt, StringComparison.OrdinalIgnoreCase)) || 
                        (!string.IsNullOrEmpty(a.Text) && a.Text.Contains(rt, StringComparison.OrdinalIgnoreCase))
                    ))
                ));
            }

            if (amenities != null && amenities.Any())
            {
                result = result.Where(r => amenities.All(am => 
                    r.Amenities != null && r.Amenities.Any(a => 
                        !string.IsNullOrEmpty(a.Text) && a.Text.Contains(am, StringComparison.OrdinalIgnoreCase)
                    )
                ));
            }

            return result.ToList();
        }

        public async Task<RoomListing?> GetRoomByIdAsync(int id)
        {
            var room = await _context.RoomListings
                                     .Include(r => r.RoomImages)
                                     .FirstOrDefaultAsync(r => r.Id == id);
            
            if (room != null)
            {
                PopulateListingMetadata(room);
            }
            
            return room;
        }

        /// <summary>
        /// Tạo bổ sung siêu dữ liệu (Metadata) và tiện ích (Amenities) động dựa trên dữ liệu thật của phòng trọ
        /// </summary>
        private void PopulateListingMetadata(RoomListing room)
        {
            // Thiết lập giá trị mặc định cho Rating để giữ giao diện đẹp mắt (không trống)
            room.Rating = 4.9;
            room.IsFavorite = false;

            // Nếu bài đăng chưa có ảnh đại diện gốc, tự động lấy ảnh đầu tiên trong danh sách ảnh phụ tải lên
            if (string.IsNullOrEmpty(room.ImageUrl))
            {
                if (room.RoomImages != null && room.RoomImages.Any())
                {
                    room.ImageUrl = room.RoomImages.First().ImageUrl;
                }
                else
                {
                    // Ảnh placeholder mặc định khi bài viết chưa tải lên ảnh nào
                    room.ImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&q=80&w=600";
                }
            }

            // Tạo danh sách tiện nghi động để hiển thị các Icon sang xịn mịn ở giao diện
            room.Amenities = new List<RoomAmenity>
            {
                // Tiện ích diện tích
                new RoomAmenity { Icon = "straighten", Text = $"{room.Area}m²", Title = "Diện tích" }
            };

            // Tiện ích Loại phòng tự động suy luận qua tiêu đề
            if (!string.IsNullOrEmpty(room.Title) && room.Title.Contains("Studio", StringComparison.OrdinalIgnoreCase))
            {
                room.Amenities.Add(new RoomAmenity { Icon = "bed", Text = "Studio", Title = "Studio" });
            }
            else if (!string.IsNullOrEmpty(room.Title) && (room.Title.Contains("Nguyên căn", StringComparison.OrdinalIgnoreCase) || room.Title.Contains("Villa", StringComparison.OrdinalIgnoreCase)))
            {
                room.Amenities.Add(new RoomAmenity { Icon = "home", Text = "Nguyên căn", Title = "Nguyên căn" });
            }
            else
            {
                room.Amenities.Add(new RoomAmenity { Icon = "bed", Text = "Phòng chung", Title = "Phòng chung" });
            }

            // Các tiện ích mặc định cao cấp cho mọi phòng trọ
            room.Amenities.Add(new RoomAmenity { Icon = "wifi", Text = "WiFi tốc độ cao", Title = "WiFi" });
            room.Amenities.Add(new RoomAmenity { Icon = "ac_unit", Text = "Điều hòa", Title = "Điều hòa" });
        }
    }
}
