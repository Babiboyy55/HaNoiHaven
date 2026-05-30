using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using nhatro.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace nhatro.Controllers
{
    public class HomeController : Controller
    {
        private readonly nhatro.Services.IRoomService _roomService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(nhatro.Services.IRoomService roomService, UserManager<ApplicationUser> userManager)
        {
            _roomService = roomService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh sách bài đăng từ database
            var listings = await _roomService.GetFeaturedRoomsAsync();
            // Lấy tối đa 3 bài đăng mới nhất để hiển thị ở trang chủ
            var featuredListings = listings.Take(3).ToList();
            return View(featuredListings);
        }

        public async Task<IActionResult> Explore(
            string? query = null,
            long? minPrice = null,
            long? maxPrice = null,
            string[]? roomTypes = null,
            string[]? amenities = null,
            string? distance = null,
            string? priceRange = null)
        {
            // Ánh xạ priceRange từ trang chủ sang minPrice và maxPrice
            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "under-3m":
                        maxPrice = 3000000;
                        break;
                    case "3m-5m":
                        minPrice = 3000000;
                        maxPrice = 5000000;
                        break;
                    case "5m-10m":
                        minPrice = 5000000;
                        maxPrice = 10000000;
                        break;
                    case "over-10m":
                        minPrice = 10000000;
                        break;
                }
            }

            var roomListings = await _roomService.GetFeaturedRoomsAsync(query, minPrice, maxPrice, roomTypes, amenities, distance);
            
            ViewBag.Query = query;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.RoomTypes = roomTypes ?? Array.Empty<string>();
            ViewBag.Amenities = amenities ?? Array.Empty<string>();
            ViewBag.Distance = distance;
            ViewBag.PriceRange = priceRange;

            return View(roomListings);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            // Lấy thông tin chủ nhà (chủ trọ) từ hệ thống Identity
            if (!string.IsNullOrEmpty(room.OwnerId))
            {
                var owner = await _userManager.FindByIdAsync(room.OwnerId);
                ViewBag.OwnerName = owner?.FullName ?? "Chủ trọ HanoiHaven";
                ViewBag.OwnerPhone = owner?.PhoneNumber ?? "Đang cập nhật";
            }
            else
            {
                ViewBag.OwnerName = "Chủ trọ HanoiHaven";
                ViewBag.OwnerPhone = "Đang cập nhật";
            }

            return View(room);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Favorites()
        {
            // Tạm thời lấy một vài phòng để làm Mockup danh sách yêu thích cho UI
            var listings = await _roomService.GetFeaturedRoomsAsync();
            var favoriteListings = listings.Take(2).ToList(); 
            
            // Giả lập trạng thái đã lưu cho màn hình Favorites
            foreach(var r in favoriteListings) {
                r.IsFavorite = true;
            }

            return View(favoriteListings);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
