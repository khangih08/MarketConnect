using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Market> Markets { get; set; } = null!;
        public DbSet<ProductMarket> ProductMarkets { get; set; } = null!;
        public DbSet<Wishlist> Wishlists { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<OtpVerification> OtpVerifications { get; set; } = null!;
        public DbSet<ProductComment> ProductComments { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProductMarket>(entity =>
            {
                entity.HasKey(pm => new { pm.MarketId, pm.ProductId });

                // Composite Index cho lọc theo market + phân trang theo created_at
                entity.HasIndex(pm => new { pm.MarketId, pm.CreatedAt, pm.ProductId })
                      .HasDatabaseName("idx_pm_market_created_product");

                entity.HasOne(pm => pm.Market)
                      .WithMany(m => m.ProductMarkets)
                      .HasForeignKey(pm => pm.MarketId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pm => pm.Product)
                      .WithMany(p => p.ProductMarkets)
                      .HasForeignKey(pm => pm.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
