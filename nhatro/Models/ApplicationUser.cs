using Microsoft.AspNetCore.Identity;

namespace nhatro.Models
{
    // Kế thừa IdentityUser để lấy sẵn các trường Email, PasswordHash, PhoneNumber...
    public class ApplicationUser : IdentityUser
    {
        // Thêm các trường Custom mà Form đăng ký yêu cầu
        public string FullName { get; set; } = string.Empty;
    }
}