using GameStore.Models;
using System.Collections.Generic;

namespace HouseRent.Models
{
    public class Poster
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public int PosterTypeID { get; set; }
        public PosterType PosterType { get; set; }

        public int? MinRentDays { get; set; }
        public int Price { get; set; }
        public int? MaxPayTerms { get; set; }
        public string Image { get; set; }

        public double Raiting { get; set; } = 0;

        // Поля для типу оголошення
        public bool IsRental { get; set; }
        public bool IsSale { get; set; }
        public bool IsLongTermRental { get; set; }
        public bool IsLongTermWithBuyout { get; set; }
        public bool IsShortTermRental { get; set; }
        public bool IsFullPayment { get; set; }
        public bool IsInstallmentPayment { get; set; }
        public string ContractTemplateImage { get; set; } // Оригінальний .docx
        public string ContractTemplatePdf { get; set; } // Новий PDF
        public string ContactDetails { get; set; }
        public string PaymentAccount { get; set; }

        public ICollection<Photo> Photos { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Raiting> Raitings { get; set; }

        public int OwnerID { get; set; }
        public User Owner { get; set; }

        public int PosterStatusID { get; set; }
        public PosterStatus PosterStatus { get; set; }
    }
}