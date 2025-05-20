using GameStore.Models;
using System.Linq;

namespace GameStore.Data
{
    public class DBInitializer
    {
        public static void Initialize(HouseRentContext context)
        {
            //context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            if (context.Roles.Any())
            {
                return;
            }


            //Ролі
            var roles = new Role[]
            {
                new Role{ Name = "Адміністратор"},
                new Role{ Name = "Продавець"},
                new Role{ Name = "Покупець"}
            };
            
            foreach (Role r in roles)
            {
                context.Roles.Add(r);
            }
            context.SaveChanges();

            //Статуси замовлення
            var orderStatuses = new OrderStatus[]
            {
                new OrderStatus {Name = "Заявка"},
                new OrderStatus {Name = "Відхилене"},
                new OrderStatus{ Name = "Прийнятий"},
                new OrderStatus{ Name = "Завершене"},
                new OrderStatus{ Name = "Перевірка"},
                new OrderStatus{ Name = "В процесі"}
            };

            foreach (OrderStatus os in orderStatuses)
            {
                context.OrderStatuses.Add(os);
            }

            context.SaveChanges();

            //Статуси оплати
            var paymentStatuses = new PaymentStatus[]
            {
                new PaymentStatus{ Name = "Виконана"},
                new PaymentStatus{ Name = "Невиконана"}
            };

            foreach (PaymentStatus ps in paymentStatuses)
            {
                context.PaymentStatuses.Add(ps);
            }

            context.SaveChanges();

            //Статуси оголошення
            var posterStatuses = new PosterStatus[]
            {
                new PosterStatus{ Name = "Модерація"},
                new PosterStatus{ Name = "Активне"},
                new PosterStatus{ Name = "Неактивне"},
                new PosterStatus{ Name = "Відхилине"},
                new PosterStatus{ Name = "В процесі оформлення"},
                new PosterStatus{ Name = "В процесі"}
            };

            foreach (PosterStatus  ps in posterStatuses)
            {
                context.PosterStatuses.Add(ps);
            }

            context.SaveChanges();

            //Статуси оголошення
            var posterType = new PosterType[]
            {
                new PosterType{ Name = "Продаж"},
                new PosterType{ Name = "Оренда (на день)"},
                new PosterType{ Name = "Оренда (довгострокова)"},
                new PosterType{ Name = "Оренда (довгострокова з викупом)"}
            };  

            foreach (PosterType pt in posterType)
            {
                context.PosterType.Add(pt);
            }

            context.SaveChanges();

            //Статуси оголошення
            var users = new User[]
            {
                new User { Login ="admin", Password = "admin", FirstName = "" , LastName = "", Avatar = "/img/men_avatar.png", Email = "zuckonit1@gmail.com", Card = "1234 5678 4332 5544"}
            };

            foreach (User u in users)
            {
                context.Users.Add(u);
            }

            context.SaveChanges();

        }
    }
}
