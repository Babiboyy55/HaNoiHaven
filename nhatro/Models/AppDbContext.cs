using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace nhatro.Models
{
    // ĐỔI TẠI ĐÂY: Kế thừa IdentityDbContext thay vì DbContext
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Khai báo các bảng khác của bạn ở đây (ví dụ: RoomListing)
        public DbSet<RoomListing> RoomListings { get; set; }
        public DbSet<RoomImage> RoomImages { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RentalRequest> RentalRequests { get; set; }
    }
}