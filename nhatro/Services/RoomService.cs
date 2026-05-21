using System.Collections.Generic;
using System.Threading.Tasks;
using nhatro.Models;

using System.Linq;

namespace nhatro.Services
{
    public class RoomService : IRoomService
    {
        public async Task<IEnumerable<RoomListing>> GetFeaturedRoomsAsync(
            string? query = null,
            long? minPrice = null,
            long? maxPrice = null,
            string[]? roomTypes = null,
            string[]? amenities = null,
            string? distance = null)
        {
            // Tạm thời giả lập gọi đến Database mất một khoảng thời gian nhỏ
            await Task.Delay(100);

            var list = new List<RoomListing>
            {
                new RoomListing
                {
                    Id = 1,
                    Title = "Studio Tối Giản Ven Hồ",
                    Location = "Hồ Tây, Tây Hồ",
                    Price = 8500000,
                    ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAfSF_Rb3Ro4b41gf4oRLDEg9LlxjqtHh7MWXQaTqz_mShkFmt8z_AVN8QtWGR7Ml7QdFQBBBKcgS69dYqzAKbNKyBGblTxWauyxOmOWveeZYH49Uff6X4so6XfgQmRuUYq4yTVw3tZFOXQn0EQInX3-M9TMwDovSvH8gz6Emp2VvyUovVQTUP5xb1NduCXyP9c_wubc84r25tS4yVXhOHQza0cRMnJEhdjnddM4oG5MkU2D-zrUs5ALY2T9l_REoXyyLMLJMvhcleE",
                    Rating = 4.9,
                    StatusBadge = null,
                    IsFavorite = false,
                    Amenities = new List<RoomAmenity>
                    {
                        new RoomAmenity { Icon = "bed", Text = "1 Giường", Title = "Studio" },
                        new RoomAmenity { Icon = "bathtub", Text = "1 WC", Title = "Private Bath" },
                        new RoomAmenity { Icon = "straighten", Text = "45m²", Title = "Square Meters" }
                    }
                },
                new RoomListing
                {
                    Id = 2,
                    Title = "Phòng Hiện Đại Trong Villa",
                    Location = "Xuân Diệu, Tây Hồ",
                    Price = 5200000,
                    ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuCVqVO9f4LdVUxst8cP2QDeypibbZEXYx-o_Fl-xE9r-BTJxBr78VIpje9u580cWcp0QEMbE8u1UXXm4SwRTr9EBi0DVxW3HqusKxSgA1O7OEuxB-kJqrFjGyPWGfYe7W93opgZKW_qOT-CwZmJPrebPOp3eLhAhgmY__fwjLYHuZXEPZThOWGWiyl7JjWwXLnZW8uipeeC6htDdHi8eovLiX2XP1x1DmlrHEU8qVuOmg5YuUVXSa7BBAfIJwUBYONDWaHHnUkqKpS3",
                    Rating = null,
                    StatusBadge = "Có Sẵn",
                    IsFavorite = true,
                    Amenities = new List<RoomAmenity>
                    {
                        new RoomAmenity { Icon = "bed", Text = "1 Giường" },
                        new RoomAmenity { Icon = "group", Text = "Chung" },
                        new RoomAmenity { Icon = "wifi", Text = "WiFi tốc độ cao" }
                    }
                },
                new RoomListing
                {
                    Id = 3,
                    Title = "Căn Hộ Dịch Vụ Cao Cấp",
                    Location = "Quảng An, Tây Hồ",
                    Price = 12000000,
                    ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuC-e8LB37P7AmkkYfKvhCi65DAppdygfDRjAYjTf7dWlleKsZ7MJafvqAvWDyIGx0IIKrKnB4H8KStQYMWfELI9BjocjtGISSkYDCD6sIPpZm7Gb7ZFiP0RTE5plm6vcpLNvnPKGjNAhJNc_294oE0WokQQ9wgeiHEmt7sTxWcsEucMMMshV9wnp6RSfFJ4dwDjR9_Ml3QfAN4Rs2ON9TKZHeVwxHWcvVMzZ1RoFnaxEERiJwGLHw596T8a0cXCW_gKQRA3K1uxn16d",
                    Rating = null,
                    StatusBadge = null,
                    IsFavorite = false,
                    Amenities = new List<RoomAmenity>
                    {
                        new RoomAmenity { Icon = "bed", Text = "2 Giường", Title = "Nguyên căn" },
                        new RoomAmenity { Icon = "local_laundry_service", Text = "Giặt/Sấy" },
                        new RoomAmenity { Icon = "pool", Text = "Hồ bơi" },
                        new RoomAmenity { Icon = "ac_unit", Text = "Điều hòa" }
                    }
                }
            };

            var result = list.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                result = result.Where(r => 
                    r.Location.Contains(query, System.StringComparison.OrdinalIgnoreCase) || 
                    r.Title.Contains(query, System.StringComparison.OrdinalIgnoreCase)
                );
            }

            if (minPrice.HasValue)
            {
                result = result.Where(r => r.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                result = result.Where(r => r.Price <= maxPrice.Value);
            }

            if (roomTypes != null && roomTypes.Any())
            {
                result = result.Where(r => roomTypes.Any(rt => 
                    r.Title.Contains(rt, System.StringComparison.OrdinalIgnoreCase) || 
                    r.Amenities.Any(a => (a.Title != null && a.Title.Contains(rt, System.StringComparison.OrdinalIgnoreCase)) || a.Text.Contains(rt, System.StringComparison.OrdinalIgnoreCase))
                ));
            }

            if (amenities != null && amenities.Any())
            {
                result = result.Where(r => amenities.All(am => 
                    r.Amenities.Any(a => a.Text.Contains(am, System.StringComparison.OrdinalIgnoreCase))
                ));
            }

            return result.ToList();
        }
    }
}
