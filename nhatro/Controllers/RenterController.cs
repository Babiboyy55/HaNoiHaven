using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using nhatro.Models;
using System.Threading.Tasks;

namespace nhatro.Controllers
{
    [Authorize(Roles = "Renter")]
    public class RenterController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RenterController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // TÍNH NĂNG TÌM KIẾM, LỌC, XEM CHI TIẾT (Sẽ được viết ở HomeController để Khách vãng lai cũng xem được)

        // 1. Xem danh sách phòng đã lưu
        public IActionResult SavedRooms()
        {
            return View();
        }

        // 2. Quản lý Yêu cầu thuê / Đặt cọc
        public IActionResult MyRequests()
        {
            return View();
        }
        
        // 3. Xem Hợp đồng và Hóa đơn cần thanh toán
        public IActionResult MyContracts()
        {
            return View();
        }

        // 4. Gửi & Theo dõi yêu cầu sửa chữa (Bảo trì)
        public IActionResult Maintenance()
        {
            return View();
        }

        // 5. Quản lý các đánh giá, nhận xét của mình
        public IActionResult MyReviews()
        {
            return View();
        }
    }
}