using API.Store.Shared;
using Microsoft.EntityFrameworkCore;


namespace API.Store.Data
{
    public class APIStoreContext : DbContext
    {
        public APIStoreContext(DbContextOptions Options):base(Options)
        {
            
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite();
        }
    }
}
