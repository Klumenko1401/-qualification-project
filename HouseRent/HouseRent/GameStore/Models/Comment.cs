using HouseRent.Models;
using System;

namespace GameStore.Models
{
    public class Comment
    {
        public int ID { get; set; }
        public string Description { get; set; }
        public int PosterID { get; set; }
        public Poster Poster { get; set; }
        public int UserID { get; set; } 
        public User User { get; set; }
        public DateTime PublicationDate { get; set; }   

    }
}
