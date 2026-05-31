using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using nhatro.Models;
using System;
using Microsoft.EntityFrameworkCore;

namespace nhatro.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment; // Bổ sung môi trường web

        // Tiêm IWebHostEnvironment vào Controller
        public OwnerController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 1. Số tin đăng hoạt động ("Đã đăng")
            var activeListingsCount = await _context.RoomListings
                .Where(r => r.OwnerId == user.Id && r.StatusBadge == "Đã đăng")
                .CountAsync();

            // 2. Số phòng vật lý đang trống
            var vacantRoomsCount = await _context.Rooms
                .Where(r => r.OwnerId == user.Id && r.Status == "Trống")
                .CountAsync();

            // 3. Doanh thu dự kiến từ các phòng đang cho thuê
            var expectedRevenue = await _context.Rooms
                .Where(r => r.OwnerId == user.Id && r.Status == "Đang thuê")
                .SumAsync(r => r.RentPrice);

            // 4. Lấy danh sách 5 bài đăng tin gần đây nhất của Chủ trọ này
            var recentListings = await _context.RoomListings
                .Where(r => r.OwnerId == user.Id)
                .OrderByDescending(r => r.Id)
                .Take(5)
                .ToListAsync();

            ViewData["ActiveListingsCount"] = activeListingsCount;
            ViewData["VacantRoomsCount"] = vacantRoomsCount;
            ViewData["ExpectedRevenue"] = expectedRevenue;

            return View(recentListings);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new RoomListing());
        }

        [HttpPost]
        // Bổ sung tham số List<IFormFile> uploadedFiles để hứng nhiều ảnh từ form
        public async Task<IActionResult> Create(RoomListing model, List<IFormFile> uploadedFiles)
        {
            // Bỏ qua kiểm tra các trường tự động gán ở Backend
            ModelState.Remove("OwnerId");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("RoomImages");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                model.OwnerId = user.Id;
                model.StatusBadge = "Chờ duyệt";
                
                // 1. Lưu thông tin phòng trước để DB tạo ra ID (Mã bài đăng)
                _context.RoomListings.Add(model);
                await _context.SaveChangesAsync(); 

                // 2. Bắt đầu xử lý danh sách ảnh tải lên
                if (uploadedFiles != null && uploadedFiles.Count > 0)
                {
                    // Đường dẫn tới thư mục wwwroot/uploads
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    bool isFirstImage = true;

                    foreach (var file in uploadedFiles)
                    {
                        if (file.Length > 0)
                        {
                            // Đổi tên file ngẫu nhiên để tránh bị trùng đè tên
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            // Copy file từ RAM vào ổ cứng (thư mục wwwroot/uploads)
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }

                            // Lưu đường dẫn ảnh vào bảng RoomImage
                            var roomImage = new RoomImage
                            {
                                ImageUrl = "/uploads/" + uniqueFileName,
                                RoomListingId = model.Id // Móc nối ảnh này với bài đăng vừa tạo
                            };
                            _context.RoomImages.Add(roomImage);

                            // MẸO: Lấy ảnh đầu tiên làm ảnh bìa (Thumbnail) cho bài đăng
                            if (isFirstImage)
                            {
                                model.ImageUrl = roomImage.ImageUrl;
                                _context.RoomListings.Update(model);
                                isFirstImage = false;
                            }
                        }
                    }
                    // Đồng bộ tất cả ảnh xuống DB
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Index");
            }
            
            return View(model);
        }

        // HTTP GET: Trang quản lý danh sách bài đăng
        [HttpGet]
        public async Task<IActionResult> ManageListings()
        {
            // Lấy thông tin tài khoản đang đăng nhập
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Truy vấn lấy danh sách bài đăng thuộc về Owner này
            var myListings = await _context.RoomListings
                                           .Where(r => r.OwnerId == user.Id)
                                           .ToListAsync();

            return View(myListings);
        }

        // --- 1. TÍNH NĂNG ẨN/HIỆN BÀI ĐĂNG ---
        [HttpPost]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            var room = await _context.RoomListings.FindAsync(id);
            if (room != null)
            {
                // Đảo ngược trạng thái
                room.StatusBadge = (room.StatusBadge == "Đã đăng") ? "Đã ẩn" : "Đã đăng";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageListings));
        }

        // --- 2. TÍNH NĂNG XÓA BÀI ĐĂNG ---
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.RoomListings.FindAsync(id);
            if (room != null)
            {
                _context.RoomListings.Remove(room);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageListings));
        }

        // --- 3. TÍNH NĂNG SỬA BÀI ĐĂNG (MỞ FORM) ---
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.RoomListings.FindAsync(id);
            if (room == null) return NotFound();
            return View(room);
        }

        // --- 4. TÍNH NĂNG SỬA BÀI ĐĂNG (LƯU DỮ LIỆU) ---
        [HttpPost]
        public async Task<IActionResult> Edit(int id, RoomListing model)
        {
            // Bỏ qua kiểm tra các trường tự động
            ModelState.Remove("OwnerId");
            ModelState.Remove("RoomImages");

            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingRoom = await _context.RoomListings.FindAsync(id);
                if (existingRoom == null) return NotFound();

                // Cập nhật thông tin mới
                existingRoom.Title = model.Title;
                existingRoom.Location = model.Location;
                existingRoom.Price = model.Price;
                existingRoom.Area = model.Area;
                
                // Nếu người dùng có nhập Link ảnh mới thì cập nhật
                if (!string.IsNullOrEmpty(model.ImageUrl))
                {
                    existingRoom.ImageUrl = model.ImageUrl;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageListings));
            }
            return View(model);
        }

        // --- QUẢN LÝ PHÒNG VẬT LÝ ---
        [HttpGet]
        public async Task<IActionResult> ManageRooms()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var myRooms = await _context.Rooms
                                        .Where(r => r.OwnerId == user.Id)
                                        .ToListAsync();
            return View(myRooms);
        }

        // ==========================================
        // QUẢN LÝ PHÒNG VẬT LÝ (THÊM, SỬA, XÓA)
        // ==========================================

        // --- 1. THÊM PHÒNG MỚI (MỞ FORM) ---
        [HttpGet]
        public IActionResult CreateRoom()
        {
            return View(new Room());
        }

        // --- 2. THÊM PHÒNG MỚI (LƯU DỮ LIỆU) ---
        [HttpPost]
        public async Task<IActionResult> CreateRoom(Room model)
        {
            ModelState.Remove("OwnerId"); // Bỏ qua validate tự động
            
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                model.OwnerId = user.Id;
                
                _context.Rooms.Add(model);
                await _context.SaveChangesAsync();
                
                return RedirectToAction(nameof(ManageRooms));
            }
            return View(model);
        }

        // --- 3. SỬA PHÒNG (MỞ FORM) ---
        [HttpGet]
        public async Task<IActionResult> EditRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            
            return View(room);
        }

        // --- 4. SỬA PHÒNG (LƯU DỮ LIỆU) ---
        [HttpPost]
        public async Task<IActionResult> EditRoom(int id, Room model)
        {
            ModelState.Remove("OwnerId");
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingRoom = await _context.Rooms.FindAsync(id);
                if (existingRoom == null) return NotFound();

                // Cập nhật các trường
                existingRoom.RoomName = model.RoomName;
                existingRoom.PropertyType = model.PropertyType;
                existingRoom.RentPrice = model.RentPrice;
                existingRoom.Status = model.Status;
                existingRoom.TenantName = model.TenantName;
                existingRoom.TenantPhone = model.TenantPhone;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageRooms));
            }
            return View(model);
        }

        // --- 5. XÓA PHÒNG ---
        [HttpPost]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageRooms));
        }

        // ==========================================
        // QUẢN LÝ HỢP ĐỒNG THUÊ (CONTRACTS)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Contracts()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var contracts = await _context.Contracts
                .Include(c => c.Room)
                .Where(c => c.OwnerId == user.Id)
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            return View(contracts);
        }

        [HttpGet]
        public async Task<IActionResult> CreateContract()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var rooms = await _context.Rooms
                .Where(r => r.OwnerId == user.Id)
                .ToListAsync();

            ViewBag.Rooms = rooms;
            return View(new Contract());
        }

        [HttpPost]
        public async Task<IActionResult> CreateContract(Contract model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ModelState.Remove("OwnerId");
            ModelState.Remove("Room");

            if (ModelState.IsValid)
            {
                model.OwnerId = user.Id;
                _context.Contracts.Add(model);
                await _context.SaveChangesAsync();

                if (model.Status == "Đang hoạt động")
                {
                    var room = await _context.Rooms.FindAsync(model.RoomId);
                    if (room != null)
                    {
                        room.Status = "Đang thuê";
                        room.TenantName = model.TenantName;
                        room.TenantPhone = model.TenantPhone;
                        _context.Rooms.Update(room);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Tạo hợp đồng thành công!";
                return RedirectToAction(nameof(Contracts));
            }

            var rooms = await _context.Rooms
                .Where(r => r.OwnerId == user.Id)
                .ToListAsync();
            ViewBag.Rooms = rooms;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditContract(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null || contract.OwnerId != user.Id) return NotFound();

            var rooms = await _context.Rooms
                .Where(r => r.OwnerId == user.Id)
                .ToListAsync();

            ViewBag.Rooms = rooms;
            return View(contract);
        }

        [HttpPost]
        public async Task<IActionResult> EditContract(int id, Contract model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (id != model.Id) return NotFound();

            ModelState.Remove("OwnerId");
            ModelState.Remove("Room");

            if (ModelState.IsValid)
            {
                var existingContract = await _context.Contracts.FindAsync(id);
                if (existingContract == null || existingContract.OwnerId != user.Id) return NotFound();

                int oldRoomId = existingContract.RoomId;

                existingContract.RoomId = model.RoomId;
                existingContract.TenantName = model.TenantName;
                existingContract.TenantPhone = model.TenantPhone;
                existingContract.StartDate = model.StartDate;
                existingContract.EndDate = model.EndDate;
                existingContract.RentPrice = model.RentPrice;
                existingContract.DepositAmount = model.DepositAmount;
                existingContract.Status = model.Status;
                existingContract.ContractTerms = model.ContractTerms;

                _context.Contracts.Update(existingContract);
                await _context.SaveChangesAsync();

                if (oldRoomId != model.RoomId || model.Status == "Đã thanh lý")
                {
                    var oldRoom = await _context.Rooms.FindAsync(oldRoomId);
                    if (oldRoom != null)
                    {
                        oldRoom.Status = "Trống";
                        oldRoom.TenantName = null;
                        oldRoom.TenantPhone = null;
                        _context.Rooms.Update(oldRoom);
                    }
                }

                if (model.Status == "Đang hoạt động")
                {
                    var currentRoom = await _context.Rooms.FindAsync(model.RoomId);
                    if (currentRoom != null)
                    {
                        currentRoom.Status = "Đang thuê";
                        currentRoom.TenantName = model.TenantName;
                        currentRoom.TenantPhone = model.TenantPhone;
                        _context.Rooms.Update(currentRoom);
                    }
                }
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật hợp đồng thành công!";
                return RedirectToAction(nameof(Contracts));
            }

            var rooms = await _context.Rooms
                .Where(r => r.OwnerId == user.Id)
                .ToListAsync();
            ViewBag.Rooms = rooms;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContract(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null || contract.OwnerId != user.Id) return NotFound();

            if (contract.Status == "Đang hoạt động")
            {
                var room = await _context.Rooms.FindAsync(contract.RoomId);
                if (room != null)
                {
                    room.Status = "Trống";
                    room.TenantName = null;
                    room.TenantPhone = null;
                    _context.Rooms.Update(room);
                }
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa hợp đồng thành công!";
            return RedirectToAction(nameof(Contracts));
        }

        // ==========================================
        // QUẢN LÝ HÓA ĐƠN & ĐIỆN NƯỚC (INVOICES)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Invoices(int? month, int? year, string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var query = _context.Invoices
                .Include(i => i.Room)
                .Where(i => i.OwnerId == user.Id)
                .AsQueryable();

            if (month.HasValue && month.Value > 0)
            {
                query = query.Where(i => i.BillingMonth == month.Value);
            }
            if (year.HasValue && year.Value > 0)
            {
                query = query.Where(i => i.BillingYear == year.Value);
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var invoices = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

            var activeRooms = await _context.Rooms
                .Where(r => r.OwnerId == user.Id && r.Status == "Đang thuê")
                .ToListAsync();

            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
            ViewBag.SelectedStatus = status;
            ViewBag.ActiveRooms = activeRooms;

            return View(invoices);
        }

        [HttpGet]
        public async Task<IActionResult> RecordUtilities(int roomId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null || room.OwnerId != user.Id) return NotFound();

            var lastInvoice = await _context.Invoices
                .Where(i => i.RoomId == roomId)
                .OrderByDescending(i => i.BillingYear)
                .ThenByDescending(i => i.BillingMonth)
                .FirstOrDefaultAsync();

            double defaultElecOld = lastInvoice?.ElectricityNew ?? 0;
            double defaultWaterOld = lastInvoice?.WaterNew ?? 0;

            var model = new Invoice
            {
                RoomId = roomId,
                RoomPrice = room.RentPrice,
                TenantName = room.TenantName ?? "Khách thuê",
                ElectricityOld = defaultElecOld,
                ElectricityNew = defaultElecOld,
                WaterOld = defaultWaterOld,
                WaterNew = defaultWaterOld,
                BillingMonth = DateTime.Today.Month,
                BillingYear = DateTime.Today.Year
            };

            ViewBag.RoomName = room.RoomName;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RecordUtilities(Invoice model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ModelState.Remove("OwnerId");
            ModelState.Remove("Room");
            ModelState.Remove("TenantName");

            if (model.ElectricityNew < model.ElectricityOld)
            {
                ModelState.AddModelError("ElectricityNew", "Chỉ số điện mới phải lớn hơn hoặc bằng chỉ số cũ!");
            }
            if (model.WaterNew < model.WaterOld)
            {
                ModelState.AddModelError("WaterNew", "Chỉ số nước mới phải lớn hơn hoặc bằng chỉ số cũ!");
            }

            var room = await _context.Rooms.FindAsync(model.RoomId);
            if (room == null || room.OwnerId != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                model.OwnerId = user.Id;
                model.TenantName = room.TenantName ?? "Khách thuê";
                
                decimal elecCost = (decimal)(model.ElectricityNew - model.ElectricityOld) * model.ElectricityPrice;
                decimal waterCost = (decimal)(model.WaterNew - model.WaterOld) * model.WaterPrice;
                model.TotalAmount = model.RoomPrice + elecCost + waterCost + model.ServiceFees;

                _context.Invoices.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ghi nhận số điện nước và tạo hóa đơn thành công!";
                return RedirectToAction(nameof(Invoices));
            }

            ViewBag.RoomName = room.RoomName;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> InvoiceDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var invoice = await _context.Invoices
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null || invoice.OwnerId != user.Id) return NotFound();

            return View(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null || invoice.OwnerId != user.Id) return NotFound();

            invoice.Status = "Đã thanh toán";
            invoice.PaymentDate = DateTime.Now;

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật trạng thái: Đã thanh toán!";
            return RedirectToAction(nameof(Invoices));
        }

        // ==========================================
        // NHẮN TIN VỚI KHÁCH THUÊ (CHAT / MESSAGES)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Messages(string? partnerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var requestTenants = await _context.RentalRequests
                .Include(r => r.RoomListing)
                .Where(r => r.RoomListing != null && r.RoomListing.OwnerId == user.Id)
                .Select(r => new { r.TenantId, Name = r.TenantName })
                .Distinct()
                .ToListAsync();

            var contractTenants = await _context.Contracts
                .Where(c => c.OwnerId == user.Id && !string.IsNullOrEmpty(c.TenantId))
                .Select(c => new { TenantId = c.TenantId!, Name = c.TenantName })
                .Distinct()
                .ToListAsync();

            var chatPartners = requestTenants
                .Concat(contractTenants)
                .GroupBy(p => p.TenantId)
                .Select(g => new ChatPartnerViewModel
                {
                    UserId = g.Key,
                    FullName = g.First().Name,
                    AvatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(g.First().Name)}&background=random"
                })
                .ToList();

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
                    ?? "Khách thuê";
                ViewBag.PartnerName = partnerName;
            }

            return View(messages);
        }

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

        // ==========================================
        // QUẢN LÝ BÁO HỎNG & SỬA CHỮA (MAINTENANCE)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> MaintenanceRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var requests = await _context.MaintenanceRequests
                .Include(r => r.Room)
                .Where(r => r.OwnerId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateMaintenanceStatus(int id, string status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null || request.OwnerId != user.Id) return NotFound();

            request.Status = status;
            _context.MaintenanceRequests.Update(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật trạng thái sửa chữa thành công!";
            return RedirectToAction(nameof(MaintenanceRequests));
        }
    }

    public class ChatPartnerViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string? LastMessage { get; set; }
    }
}