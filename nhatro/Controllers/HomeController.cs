using Microsoft.AspNetCore.Mvc;
using nhatro.Models;
using System.Diagnostics;

using System.Threading.Tasks;

namespace nhatro.Controllers
{
    public class HomeController : Controller
    {
        private readonly nhatro.Services.IRoomService _roomService;

        public HomeController(nhatro.Services.IRoomService roomService)
        {
            _roomService = roomService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Explore(
            string? query = null,
            long? minPrice = null,
            long? maxPrice = null,
            string[]? roomTypes = null,
            string[]? amenities = null,
            string? distance = null)
        {
            var roomListings = await _roomService.GetFeaturedRoomsAsync(query, minPrice, maxPrice, roomTypes, amenities, distance);
            
            ViewBag.Query = query;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.RoomTypes = roomTypes ?? Array.Empty<string>();
            ViewBag.Amenities = amenities ?? Array.Empty<string>();
            ViewBag.Distance = distance;

            return View(roomListings);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
