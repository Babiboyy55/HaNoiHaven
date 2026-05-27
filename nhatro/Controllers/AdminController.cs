using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace nhatro.Controllers
{
    // CÚ PHÁP QUAN TRỌNG: Chỉ những ai có Role là "Admin" mới được vào Controller này
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // Sau này bạn có thể thêm các Action khác như ManageUsers, ManageUnits... ở đây
    }
}