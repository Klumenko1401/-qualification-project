using System.Collections;
using System.Collections.Generic;

namespace GameStore.Models
{
    public class Role
    {
        public int ID { get; set; } 
        public string Name { get; set; }
        ICollection<User> Users { get; set; }  
    }
}
