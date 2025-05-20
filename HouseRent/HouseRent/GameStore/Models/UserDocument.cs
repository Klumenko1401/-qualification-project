using GameStore.Models;
using System;

namespace HouseRent.Models
{
    public class UserDocument
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; }
        public int DocumentTypeID { get; set; }
        public DocumentType DocumentType { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }
        public int DocumentStatusID { get; set; }
        public DocumentStatus DocumentStatus { get; set; }
        public string Comment { get; set; }
    }
}
