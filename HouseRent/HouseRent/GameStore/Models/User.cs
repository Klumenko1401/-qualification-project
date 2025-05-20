using HouseRent.Models;
using System.Collections.Generic;

namespace GameStore.Models
{
    public class User
    {
        public int ID { get; set; } 
        public string Login { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Avatar { get; set; }
        public string Email { get; set; }
        public string Card { get; set; }
        public int RoleID { get; set; }
        public Role Role { get; set; }
        public string FullName { get => FirstName + " " + LastName; }
        public ICollection<Poster> Posters { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<Raiting> Raitings { get; set; }
        public int? VerificationStatusID { get; set; } // Nullable, якщо користувач ще не перевірений
        public DocumentStatus VerificationStatus { get; set; }
        public ICollection<UserDocument> UserDocuments { get; set; }
    }
}
