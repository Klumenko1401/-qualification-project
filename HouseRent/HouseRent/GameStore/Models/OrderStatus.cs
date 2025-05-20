using HouseRent.Models;
using System.Collections;
using System.Collections.Generic;

namespace GameStore.Models
{
    public class OrderStatus
    {
        public int ID { get; set; } 
        public string Name { get; set; }
        ICollection<Order> Orders{ get; set; }  
    }
}
