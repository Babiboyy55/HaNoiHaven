using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using nhatro.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace nhatro.Controllers
{
    public class HomeController : Controller
    {
        private readonly nhatro.Services.IRoomService _roomService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public HomeController(nhatro.Services.IRoomService roomService, UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _roomService = roomService;
            _userManager = userManager;
            _context = context;
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

        // =====================================================
        // TENANT: HỢP ĐỒNG, HÓA ĐƠN & YÊU CẦU SỬA CHỮA
        // =====================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyContracts()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Tìm hợp đồng của người dùng (theo TenantId hoặc Phone)
            var contracts = await _context.Contracts
                .Include(c => c.Room)
                .Where(c => c.TenantId == user.Id || (!string.IsNullOrEmpty(user.PhoneNumber) && c.TenantPhone == user.PhoneNumber))
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            // Tự động liên kết TenantId nếu khớp SĐT nhưng chưa gán ID
            bool hasUpdates = false;
            foreach (var c in contracts)
            {
                if (string.IsNullOrEmpty(c.TenantId))
                {
                    c.TenantId = user.Id;
                    _context.Contracts.Update(c);
                    hasUpdates = true;
                }
            }
            if (hasUpdates)
            {
                await _context.SaveChangesAsync();
            }

            var roomIds = contracts.Select(c => c.RoomId).ToList();

            // Lấy hóa đơn
            var invoices = await _context.Invoices
                .Include(i => i.Room)
                .Where(i => roomIds.Contains(i.RoomId))
                .OrderByDescending(i => i.BillingYear)
                .ThenByDescending(i => i.BillingMonth)
                .ToListAsync();

            // Lấy yêu cầu sửa chữa
            var maintenanceRequests = await _context.MaintenanceRequests
                .Include(m => m.Room)
                .Where(m => m.TenantId == user.Id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.Invoices = invoices;
            ViewBag.MaintenanceRequests = maintenanceRequests;

            return View(contracts);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PayInvoice(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            // Xác minh hóa đơn này thuộc về phòng người dùng đang thuê
            var isRoomRented = await _context.Contracts.AnyAsync(c => c.RoomId == invoice.RoomId && c.TenantId == user.Id && c.Status == "Đang hoạt động");
            if (!isRoomRented) return Forbid();

            invoice.Status = "Đã thanh toán";
            invoice.PaymentDate = DateTime.Now;

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thanh toán hóa đơn trực tuyến thành công!";
            return RedirectToAction(nameof(MyContracts));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateMaintenanceRequest(int roomId, string description)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrEmpty(description))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mô tả sự cố!";
                return RedirectToAction(nameof(MyContracts));
            }

            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();

            var request = new MaintenanceRequest
            {
                RoomId = roomId,
                TenantId = user.Id,
                Description = description,
                Status = "Chờ xử lý",
                CreatedAt = DateTime.Now,
                OwnerId = room.OwnerId
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Gửi yêu cầu sửa chữa thành công! Chủ nhà đã được thông báo.";
            return RedirectToAction(nameof(MyContracts));
        }

        // =====================================================
        // TENANT: NHẮN TIN VỚI CHỦ NHÀ
        // =====================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Messages(string? partnerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Tìm danh sách chủ nhà (Owners) để Tenant chat
            // 1. Từ lịch hẹn xem phòng
            var requestOwners = await _context.RentalRequests
                .Include(r => r.RoomListing)
                .Where(r => r.TenantId == user.Id && r.RoomListing != null)
                .Select(r => r.RoomListing!.OwnerId)
                .Distinct()
                .ToListAsync();

            // 2. Từ hợp đồng thuê phòng
            var contractOwners = await _context.Contracts
                .Where(c => c.TenantId == user.Id)
                .Select(c => c.OwnerId)
                .Distinct()
                .ToListAsync();

            // Nếu người dùng nhấp trực tiếp từ chi tiết bài đăng
            if (!string.IsNullOrEmpty(partnerId) && !requestOwners.Contains(partnerId) && !contractOwners.Contains(partnerId))
            {
                contractOwners.Add(partnerId);
            }

            var allOwnerIds = requestOwners.Concat(contractOwners).Distinct().ToList();

            var chatPartners = new List<ChatPartnerViewModel>();
            foreach (var ownerId in allOwnerIds)
            {
                var owner = await _userManager.FindByIdAsync(ownerId);
                if (owner != null)
                {
                    chatPartners.Add(new ChatPartnerViewModel
                    {
                        UserId = owner.Id,
                        FullName = owner.FullName ?? owner.Email ?? "Chủ nhà",
                        AvatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(owner.FullName ?? owner.Email ?? "Host")}&background=random"
                    });
                }
            }

            ViewBag.ChatPartners = chatPartners;
            ViewBag.SelectedPartnerId = partnerId;

            List<ChatMessage> messages = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(partnerId))
            {
                messages = await _context.ChatMessages
                    .Where(m => (m.SenderId == user.Id && m.ReceiverId == partnerId) || 
                                (m.SenderId == partnerId && m.ReceiverId == user.Id))
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                var unreadMessages = messages.Where(m => m.ReceiverId == user.Id && !m.IsRead).ToList();
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }

                var partnerName = chatPartners.FirstOrDefault(p => p.UserId == partnerId)?.FullName 
                    ?? (await _userManager.FindByIdAsync(partnerId))?.FullName 
                    ?? "Chủ nhà";
                ViewBag.PartnerName = partnerName;
            }

            return View(messages);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendMessage(string receiverId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(receiverId))
            {
                return BadRequest();
            }

            var message = new ChatMessage
            {
                SenderId = user.Id,
                ReceiverId = receiverId,
                Content = content,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Messages), new { partnerId = receiverId });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InvoiceDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var invoice = await _context.Invoices
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            // Xác minh hóa đơn thuộc phòng khách đang thuê
            var isRoomRented = await _context.Contracts.AnyAsync(c => c.RoomId == invoice.RoomId && c.TenantId == user.Id);
            if (!isRoomRented) return Forbid();

            return View(invoice);
        }
    }
}
