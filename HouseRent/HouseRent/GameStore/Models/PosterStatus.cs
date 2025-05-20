using HouseRent.Models;
using System.Collections;
using System.Collections.Generic;

namespace GameStore.Models
{
    public class PosterStatus
    {
        public int ID { get; set; } 
        public string Name { get; set; }
        ICollection<Poster> Posters{ get; set; }  
    }
}
