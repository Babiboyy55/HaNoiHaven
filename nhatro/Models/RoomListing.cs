namespace nhatro.Models
{
    public class RoomListing
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        
        // Badges
        public double? Rating { get; set; } 
        public string StatusBadge { get; set; } 
        public bool IsFavorite { get; set; }

        public List<RoomAmenity> Amenities { get; set; } = new List<RoomAmenity>();
    }

    public class RoomAmenity
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public string Title { get; set; } 
    }
}
