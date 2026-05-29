namespace nhatro.Models
{
    public class RoomImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        
        // Khóa ngoại liên kết với bài đăng
        public int RoomListingId { get; set; }
        public RoomListing? RoomListing { get; set; }
    }
}