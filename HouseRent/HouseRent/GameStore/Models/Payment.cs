using HouseRent.Models;
using System;
using System.Collections.Generic;

namespace GameStore.Models
{
    public class Payment
    {
        public int ID { get; set; }
        public int OrderID { get; set; }
        public Order Order { get; set; }
        public int PaymentStatusID { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime? PaymentDate { get; set; } // Зроблено nullable
        public DateTime? PaymentDueDate { get; set; } // Додано для дати, коли платіж має бути здійснений
        public int Amount { get; set; }
    }
}