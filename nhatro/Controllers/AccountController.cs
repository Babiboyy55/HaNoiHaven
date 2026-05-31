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
            // 1. Kiểm tra dữ liệu form có hợp lệ không
            if (ModelState.IsValid)
            {
                // 2. Tạo đối tượng User mới (Chú ý: map đúng FullName, Email)
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber
                };

                // 3. Thực thi lưu vào Database qua UserManager
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // 4. Gắn Role (Vai trò: Renter hoặc Owner) cho tài khoản này
                    await _userManager.AddToRoleAsync(user, model.Role);

                    // 5. Tự động đăng nhập luôn sau khi đăng ký thành công
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // 6. Điều hướng tùy theo Role
                    if (model.Role == "Owner")
                        return RedirectToAction("Index", "Owner");

                    return RedirectToAction("Index", "Home");
                }

                // NẾU LỖI (VD: Trùng email, mật khẩu yếu...): Đẩy lỗi ra màn hình
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Trả lại View nếu dữ liệu không hợp lệ
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