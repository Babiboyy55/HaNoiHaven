using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using nhatro.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace nhatro.Controllers
{
    public class RentalRequestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RentalRequestController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =====================================================
        // TENANT: Gửi yêu cầu hẹn xem phòng (POST từ Detail.cshtml)
        // =====================================================
        [HttpPost]
        [Authorize] // Bắt buộc đăng nhập mới được gửi yêu cầu
        public async Task<IActionResult> Create(int roomListingId, string preferredDate, string preferredTime, string message)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var request = new RentalRequest
            {
                RoomListingId = roomListingId,
                TenantId = user.Id,
                TenantName = user.FullName ?? user.Email ?? "Khách hàng",
                TenantPhone = user.PhoneNumber ?? "Chưa cập nhật",
                PreferredDate = DateTime.TryParse(preferredDate, out var date) ? date : DateTime.Now.AddDays(1),
                PreferredTime = preferredTime,
                Message = message,
                Status = "Chờ duyệt",
                CreatedAt = DateTime.Now
            };

            _context.RentalRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessRequest"] = "Yêu cầu hẹn xem phòng của bạn đã được gửi thành công! Chủ nhà sẽ liên hệ lại sớm nhất.";
            return RedirectToAction("Detail", "Home", new { id = roomListingId });
        }

        // =====================================================
        // OWNER: Xem danh sách yêu cầu thuê phòng của chính mình
        // =====================================================
        [HttpGet]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> MyRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Lấy tất cả các yêu cầu được gửi đến các phòng mà Chủ trọ này sở hữu
            var requests = await _context.RentalRequests
                .Include(r => r.RoomListing)
                .Where(r => r.RoomListing != null && r.RoomListing.OwnerId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        // =====================================================
        // OWNER: Phê duyệt yêu cầu xem phòng
        // =====================================================
        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request != null)
            {
                request.Status = "Đã duyệt";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xác nhận lịch hẹn xem phòng!";
            }
            return RedirectToAction(nameof(MyRequests));
        }

        // =====================================================
        // OWNER: Từ chối yêu cầu xem phòng
        // =====================================================
        [HttpPost]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.RentalRequests.FindAsync(id);
            if (request != null)
            {
                request.Status = "Đã từ chối";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã từ chối lịch hẹn.";
            }
            return RedirectToAction(nameof(MyRequests));
        }

        // =====================================================
        // TENANT: Xem lịch sử yêu cầu của bản thân
        // =====================================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var requests = await _context.RentalRequests
                .Include(r => r.RoomListing)
                .Where(r => r.TenantId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }
    }
}
