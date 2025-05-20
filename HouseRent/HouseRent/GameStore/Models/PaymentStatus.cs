using HouseRent.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GameStore.Models
{
    public class PaymentStatus
    {
        public int ID { get; set; } 
        public string Name { get; set; }    
        ICollection<Payment> Payments{ get; set; }
    }
}
