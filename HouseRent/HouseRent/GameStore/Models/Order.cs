using HouseRent.Models;
using System;
using System.Collections.Generic;

namespace GameStore.Models
{
    public class Order
    {
        public int ID { get; set; }
        public int PosterID { get; set; }
        public Poster Poster { get; set; }
        public int UserID { get; set; }
        public User User { get; set; }
        public DateTime OrderDate { get; set; }
        public int OrderStatusID { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public string ContractFilePath { get; set; } // Додано для зберігання шляху до файлу договору
        public ICollection<Payment> Payments { get; set; }
    }
}