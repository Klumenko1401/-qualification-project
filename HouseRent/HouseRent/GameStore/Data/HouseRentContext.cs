using GameStore.Models;
using HouseRent.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Data
{
    public class HouseRentContext : DbContext
    {
        public HouseRentContext(DbContextOptions<HouseRentContext> options) : base(options)
        {
        }

        public DbSet<Comment> Comments { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentStatus> PaymentStatuses { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<Poster> Posters { get; set; }
        public DbSet<PosterStatus> PosterStatuses { get; set; }
        public DbSet<PosterType> PosterType { get; set; }
        public DbSet<Raiting> Raitings { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<DocumentStatus> DocumentStatuses { get; set; }
        public DbSet<UserDocument> UserDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>().ToTable("Comment").HasOne(x => x.User).WithMany(x => x.Comments).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Order>().ToTable("Order").HasOne(x => x.User).WithMany(x => x.Orders).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OrderStatus>().ToTable("OrderStatus");
            modelBuilder.Entity<Payment>().ToTable("Payment");
            modelBuilder.Entity<PaymentStatus>().ToTable("PaymentStatus");
            modelBuilder.Entity<Photo>().ToTable("Photo");
            modelBuilder.Entity<Poster>().ToTable("Poster");
            modelBuilder.Entity<PosterStatus>().ToTable("PosterStatus");
            modelBuilder.Entity<PosterType>().ToTable("PosterType");
            modelBuilder.Entity<Raiting>().ToTable("Raiting").HasOne(x => x.User).WithMany(x => x.Raitings).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<DocumentType>().ToTable("DocumentType");
            modelBuilder.Entity<DocumentStatus>().ToTable("DocumentStatus");

            // Налаштування зв’язків для UserDocument
            modelBuilder.Entity<UserDocument>().ToTable("UserDocument");

            // Зв’язок із User
            modelBuilder.Entity<UserDocument>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserDocuments)
                .HasForeignKey(x => x.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // Зв’язок із DocumentType
            modelBuilder.Entity<UserDocument>()
                .HasOne(x => x.DocumentType)
                .WithMany(x => x.UserDocuments)
                .HasForeignKey(x => x.DocumentTypeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Зв’язок із DocumentStatus
            modelBuilder.Entity<UserDocument>()
                .HasOne(x => x.DocumentStatus)
                .WithMany(x => x.UserDocuments)
                .HasForeignKey(x => x.DocumentStatusID)
                .OnDelete(DeleteBehavior.Restrict);

            // Зв’язок VerificationStatusID у User
            modelBuilder.Entity<User>()
                .HasOne(x => x.VerificationStatus)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.VerificationStatusID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}