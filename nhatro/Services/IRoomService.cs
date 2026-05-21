using System.Collections.Generic;
using System.Threading.Tasks;
using nhatro.Models;

namespace nhatro.Services
{
    public interface IRoomService
    {
        Task<IEnumerable<RoomListing>> GetFeaturedRoomsAsync(
            string? query = null,
            long? minPrice = null,
            long? maxPrice = null,
            string[]? roomTypes = null,
            string[]? amenities = null,
            string? distance = null);
    }
}
