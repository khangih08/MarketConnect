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

        // Entities cho Chợ Việt Online
        public DbSet<Province> Provinces { get; set; } = null!;
        public DbSet<District> Districts { get; set; } = null!;
        public DbSet<Ward> Wards { get; set; } = null!;

        public DbSet<Store> Stores { get; set; } = null!;
        public DbSet<ModerationCase> ModerationCases { get; set; } = null!;
        public DbSet<ModerationRule> ModerationRules { get; set; } = null!;
        public DbSet<CategoryPriceReference> CategoryPriceReferences { get; set; } = null!;

        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; } = null!;
        public DbSet<PurchaseRequestItem> PurchaseRequestItems { get; set; } = null!;

        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<AbuseReport> AbuseReports { get; set; } = null!;

        public DbSet<AdminScope> AdminScopes { get; set; } = null!;

        public DbSet<AdPackage> AdPackages { get; set; } = null!;
        public DbSet<AdCampaign> AdCampaigns { get; set; } = null!;
        public DbSet<AdEventLog> AdEventLogs { get; set; } = null!;

        public DbSet<MobileSellerProfile> MobileSellerProfiles { get; set; } = null!;
        public DbSet<SellerAvailability> SellerAvailabilities { get; set; } = null!;
        public DbSet<LocationSample> LocationSamples { get; set; } = null!;
        public DbSet<SellerCallRequest> SellerCallRequests { get; set; } = null!;

        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<UserSession> UserSessions { get; set; } = null!;

        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<ContentVersion> ContentVersions { get; set; } = null!;
        public DbSet<ModerationActionHistory> ModerationActionHistories { get; set; } = null!;
        public DbSet<ModerationAppeal> ModerationAppeals { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProductMarket>(entity =>
            {
                entity.ToTable("ProductMarkets");
                entity.HasKey(pm => new { pm.MarketId, pm.ProductId });

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

            // Composite Index cho Store status + Market
            builder.Entity<Store>()
                .HasIndex(s => new { s.MarketId, s.Status })
                .HasDatabaseName("idx_store_market_status");

            // Index cho Moderation Status & RiskScore
            builder.Entity<ModerationCase>()
                .HasIndex(mc => new { mc.EntityType, mc.Status, mc.RiskScore })
                .HasDatabaseName("idx_modcase_type_status_risk");

            // Index cho Seller Availability location
            builder.Entity<SellerAvailability>()
                .HasIndex(sa => new { sa.IsOnline, sa.CurrentLatitude, sa.CurrentLongitude })
                .HasDatabaseName("idx_seller_online_geo");

            // Index cho Anti-Abuse IP Hash & Device Fingerprint
            builder.Entity<Review>()
                .HasIndex(r => new { r.StoreId, r.IpHash })
                .HasDatabaseName("idx_review_store_ip");
        }
    }
}
