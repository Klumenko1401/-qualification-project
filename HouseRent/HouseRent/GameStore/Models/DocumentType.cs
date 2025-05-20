using System.Collections.Generic;

namespace HouseRent.Models
{
    public class DocumentType
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public ICollection<UserDocument> UserDocuments { get; set; }
    }
}
