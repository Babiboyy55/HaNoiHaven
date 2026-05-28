using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace nhatro.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // Các tính năng sau này như: CreateRoom, EditRoom, ManageTenants sẽ được viết ở đây
    }
}