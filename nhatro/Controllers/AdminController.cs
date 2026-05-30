using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using nhatro.Models;
using Microsoft.AspNetCore.Identity;

namespace nhatro.Controllers
{
    public class UserWithRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    // CÚ PHÁP QUAN TRỌNG: Chỉ những ai có Role là "Admin" mới được vào Controller này
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Tính tổng người dùng trong hệ thống (khách hàng + chủ trọ + admin)
            var totalUsersCount = await _userManager.Users.CountAsync();

            // 2. Tính số lượng phòng vật lý đang được thuê
            var occupiedRoomsCount = await _context.Rooms
                .Where(r => r.Status == "Đang thuê")
                .CountAsync();

            // 3. Tính số bài đăng chờ phê duyệt (trạng thái khác "Đã đăng")
            var pendingListingsCount = await _context.RoomListings
                .Where(r => r.StatusBadge == "Bản nháp" || r.StatusBadge == "Chờ duyệt" || string.IsNullOrEmpty(r.StatusBadge))
                .CountAsync();

            // 4. Tổng số yêu cầu xem phòng / đặt phòng
            var rentalRequestsCount = await _context.RentalRequests.CountAsync();

            ViewData["TotalUsersCount"] = totalUsersCount;
            ViewData["OccupiedRoomsCount"] = occupiedRoomsCount;
            ViewData["PendingListingsCount"] = pendingListingsCount;
            ViewData["RentalRequestsCount"] = rentalRequestsCount;

            return View();
        }

        // ==========================================
        // 👥 1. QUẢN LÝ TÀI KHOẢN NGƯỜI DÙNG & DUYỆT CHỦ TRỌ
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var modelList = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                modelList.Add(new UserWithRolesViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Renter"
                });
            }

            return View(modelList);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                // Ngăn cản tự hạ quyền của chính mình
                if (user.UserName == User.Identity?.Name && newRole != "Admin")
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự hạ quyền Admin của chính mình!";
                    return RedirectToAction(nameof(ManageUsers));
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, newRole);
                TempData["SuccessMessage"] = $"Đã cập nhật quyền thành viên thành công!";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                if (user.UserName == User.Identity?.Name)
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình!";
                    return RedirectToAction(nameof(ManageUsers));
                }

                await _userManager.DeleteAsync(user);
                TempData["SuccessMessage"] = "Đã xóa người dùng khỏi hệ thống!";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // ==========================================
        // 🏠 2. KIỂM DUYỆT BÀI ĐĂNG (TIN ĐĂNG PHÒNG TRỌ)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ManageListings()
        {
            var listings = await _context.RoomListings
                                         .OrderByDescending(r => r.Id)
                                         .ToListAsync();
            return View(listings);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveListing(int id)
        {
            var listing = await _context.RoomListings.FindAsync(id);
            if (listing != null)
            {
                listing.StatusBadge = "Đã đăng";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã phê duyệt và đăng bài thành công!";
            }
            return RedirectToAction(nameof(ManageListings));
        }

        [HttpPost]
        public async Task<IActionResult> RejectListing(int id)
        {
            var listing = await _context.RoomListings.FindAsync(id);
            if (listing != null)
            {
                listing.StatusBadge = "Đã ẩn";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã từ chối và ẩn bài đăng!";
            }
            return RedirectToAction(nameof(ManageListings));
        }

        // ==========================================
        // ⚙️ 3. QUẢN LÝ DANH MỤC & CẤU HÌNH HỆ THỐNG
        // ==========================================
        [HttpGet]
        public IActionResult Configurations()
        {
            return View();
        }

        // ==========================================
        // ⚖️ 4. XỬ LÝ KHIẾU NẠI & TRANH CHẤP
        // ==========================================
        [HttpGet]
        public IActionResult Complaints()
        {
            return View();
        }
    }
}