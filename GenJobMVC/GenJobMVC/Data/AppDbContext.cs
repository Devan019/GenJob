using GenJobMVC.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace GenJobMVC.Data
{

    namespace MyAuthMySQL.Data
    {
        public class ApplicationDbContext : IdentityDbContext<User>
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options) { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // Set default max length = 191 for all string properties
                foreach (var entity in modelBuilder.Model.GetEntityTypes())
                {
                    foreach (var property in entity.GetProperties())
                    {
                        if (property.ClrType == typeof(string) && property.GetMaxLength() == null)
                        {
                            property.SetMaxLength(191);
                        }
                    }
                }
            }

        }
    }

}
