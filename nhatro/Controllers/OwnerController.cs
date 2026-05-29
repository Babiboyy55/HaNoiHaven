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

        public IActionResult Index()
        {
            return View();
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
                model.StatusBadge = "Đã đăng";
                
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
    }
}