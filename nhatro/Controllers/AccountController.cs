using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using nhatro.Models;
using System.Threading.Tasks;

namespace nhatro.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        // Tiêm (Inject) UserManager và SignInManager vào Controller
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Hiển thị trang Đăng Nhập
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Xử lý khi bấm nút Đăng Nhập
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Lấy thông tin user vừa đăng nhập
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    var roles = await _userManager.GetRolesAsync(user);

                    // Phân luồng dựa trên Role
                    if (roles.Contains("Admin"))
                    {
                        return RedirectToAction("Index", "Admin"); // Quản trị viên -> Bảng điều khiển Admin
                    }
                    else if (roles.Contains("Owner"))
                    {
                        // TODO: Sau này bạn tạo OwnerController thì đổi về đó
                        return RedirectToAction("Index", "Owner"); 
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home"); // Người thuê -> Trang chủ tìm phòng
                    }
                }

                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            }
            return View(model);
        }

        // Hiển thị trang Đăng Ký
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Xử lý khi bấm nút Đăng Ký
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Tạo đối tượng ApplicationUser từ dữ liệu Form
                var user = new ApplicationUser
                {
                    UserName = model.Email, // MẸO: Gán thẳng Email vào cột UserName
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber
                };

                // 2. Dùng UserManager để lưu xuống Database (Tự động mã hóa mật khẩu)
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Bảo mật: Chỉ cho phép cấp quyền Owner hoặc Renter. Ngăn chặn hacker cố tình truyền chữ "Admin"
                    if (model.Role == "Owner" || model.Role == "Renter")
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }
                    else
                    {
                        // Mặc định nếu có lỗi sẽ là người thuê
                        await _userManager.AddToRoleAsync(user, "Renter");
                    }

                    // Chuyển hướng sang trang đăng nhập
                    return RedirectToAction("Login");
                }

                // 4. Nếu có lỗi từ DB (VD: Email đã tồn tại), đẩy lỗi ra giao diện
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            
            return View(model);
        }
        
        // Đăng xuất
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}