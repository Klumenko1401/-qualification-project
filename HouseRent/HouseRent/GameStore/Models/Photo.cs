using HouseRent.Models;

namespace GameStore.Models
{
    public class Photo
    {
        public int ID { get; set; }
        public int PosterID { get; set; }
        public Poster Poster { get; set; }
        public string Image { get; set; }
    }
}
