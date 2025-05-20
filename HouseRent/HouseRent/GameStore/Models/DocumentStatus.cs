using GameStore.Models;
using System.Collections.Generic;

namespace HouseRent.Models
{
    public class DocumentStatus
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public ICollection<UserDocument> UserDocuments { get; set; }
        public ICollection<User> Users { get; set; } // Для VerificationStatusID
    }
}
