using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext db)
        {
            // 0. Tạo tất cả các bảng chưa có trong PostgreSQL bằng CREATE TABLE IF NOT EXISTS để phòng ngừa 42P01: relation does not exist
            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""Markets"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Name"" VARCHAR(255) NOT NULL,
                        ""Slug"" VARCHAR(255) NOT NULL,
                        ""ProvinceId"" INT,
                        ""DistrictId"" INT,
                        ""WardId"" INT,
                        ""Address"" VARCHAR(300),
                        ""Latitude"" DOUBLE PRECISION DEFAULT 0,
                        ""Longitude"" DOUBLE PRECISION DEFAULT 0,
                        ""OpeningHours"" VARCHAR(100) DEFAULT '05:00 - 19:00',
                        ""ManagementContact"" VARCHAR(300),
                        ""PopularCategories"" VARCHAR(500),
                        ""ImageUrl"" VARCHAR(500),
                        ""IsActive"" BOOLEAN DEFAULT TRUE,
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""ProvinceId"" INT;
                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""DistrictId"" INT;
                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""WardId"" INT;
                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""Address"" VARCHAR(300);
                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""Latitude"" DOUBLE PRECISION DEFAULT 0;
                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""Longitude"" DOUBLE PRECISION DEFAULT 0;
                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""OpeningHours"" VARCHAR(100);
                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""ManagementContact"" VARCHAR(300);
                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""PopularCategories"" VARCHAR(500);
                    ALTER TABLE ""Markets"" ADD COLUMN IF NOT EXISTS ""ImageUrl"" VARCHAR(500);

                    ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""StoreId"" INT;
                    ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Unit"" VARCHAR(50) DEFAULT 'Cái';
                    ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""PriceType"" VARCHAR(50) DEFAULT 'Fixed';
                    ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""MinOrderQuantity"" INT DEFAULT 1;
                    ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""StockStatus"" VARCHAR(50) DEFAULT 'InStock';
                    ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""SearchKeywords"" VARCHAR(500);
                    ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ModerationStatus"" INT DEFAULT 3;

                    CREATE TABLE IF NOT EXISTS ""Provinces"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Name"" VARCHAR(150) NOT NULL,
                        ""Code"" VARCHAR(20) NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS ""Districts"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""ProvinceId"" INT NOT NULL,
                        ""Name"" VARCHAR(150) NOT NULL,
                        ""Code"" VARCHAR(20) NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS ""Wards"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""DistrictId"" INT NOT NULL,
                        ""Name"" VARCHAR(150) NOT NULL,
                        ""Code"" VARCHAR(20) NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS ""Stores"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INT NOT NULL,
                        ""MarketId"" INT NOT NULL,
                        ""StoreName"" VARCHAR(200) NOT NULL,
                        ""RepresentativeName"" VARCHAR(200) NOT NULL,
                        ""VerifiedPhone"" VARCHAR(30) NOT NULL,
                        ""StallLocation"" VARCHAR(300) NOT NULL,
                        ""CategoryId"" INT NOT NULL,
                        ""Description"" TEXT,
                        ""LogoUrl"" VARCHAR(500),
                        ""CoverUrl"" VARCHAR(500),
                        ""PhotoProofUrl"" VARCHAR(500),
                        ""OpeningHours"" VARCHAR(100),
                        ""ContactChannelsJson"" TEXT,
                        ""PickupMethods"" VARCHAR(200),
                        ""Status"" INT NOT NULL DEFAULT 3,
                        ""RejectionReason"" VARCHAR(500),
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""ModerationCases"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""EntityType"" VARCHAR(50) NOT NULL,
                        ""EntityId"" INT NOT NULL,
                        ""RiskScore"" INT NOT NULL,
                        ""TriggeredRulesJson"" TEXT,
                        ""Decision"" INT NOT NULL,
                        ""Status"" INT NOT NULL,
                        ""AssignedAdminId"" INT,
                        ""AdminNotes"" VARCHAR(1000),
                        ""ContentSnapshotJson"" TEXT,
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW(),
                        ""HandledAt"" TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS ""ModerationRules"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""RuleKey"" VARCHAR(100) NOT NULL,
                        ""RuleName"" VARCHAR(200) NOT NULL,
                        ""Weight"" INT NOT NULL,
                        ""ConfigJson"" TEXT,
                        ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE
                    );

                    CREATE TABLE IF NOT EXISTS ""CartItems"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""BuyerId"" INT NOT NULL,
                        ""ProductId"" INT NOT NULL,
                        ""StoreId"" INT NOT NULL,
                        ""Quantity"" INT NOT NULL DEFAULT 1,
                        ""SelectedOptions"" VARCHAR(200),
                        ""Note"" VARCHAR(500),
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""PurchaseRequests"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""RequestCode"" VARCHAR(50) NOT NULL,
                        ""BuyerId"" INT NOT NULL,
                        ""StoreId"" INT NOT NULL,
                        ""Status"" INT NOT NULL DEFAULT 0,
                        ""BuyerName"" VARCHAR(100) NOT NULL,
                        ""BuyerPhone"" VARCHAR(30) NOT NULL,
                        ""PreferredPickupMethod"" VARCHAR(200),
                        ""Note"" VARCHAR(500),
                        ""ReferenceTotalPrice"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""PurchaseRequestItems"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""PurchaseRequestId"" INT NOT NULL,
                        ""ProductId"" INT NOT NULL,
                        ""ProductName"" VARCHAR(200) NOT NULL,
                        ""Price"" DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ""Quantity"" INT NOT NULL DEFAULT 1,
                        ""OptionsNote"" VARCHAR(200)
                    );

                    CREATE TABLE IF NOT EXISTS ""Reviews"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""BuyerId"" INT NOT NULL,
                        ""StoreId"" INT NOT NULL,
                        ""PurchaseRequestId"" INT,
                        ""RatingScore"" INT NOT NULL,
                        ""CriteriaRatingsJson"" TEXT,
                        ""Comment"" VARCHAR(1000),
                        ""MerchantReply"" VARCHAR(1000),
                        ""ReplyUpdatedAt"" TIMESTAMP,
                        ""Status"" INT NOT NULL DEFAULT 1,
                        ""IsVerifiedInteraction"" BOOLEAN DEFAULT FALSE,
                        ""RatingWeight"" DOUBLE PRECISION DEFAULT 1.0,
                        ""IpHash"" VARCHAR(64),
                        ""DeviceFingerprint"" VARCHAR(200),
                        ""EditHistoryJson"" TEXT,
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""AbuseReports"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""ReportCode"" VARCHAR(50) NOT NULL,
                        ""ReporterId"" INT NOT NULL,
                        ""TargetType"" VARCHAR(50) NOT NULL,
                        ""TargetId"" INT NOT NULL,
                        ""ViolationType"" VARCHAR(100) NOT NULL,
                        ""Description"" VARCHAR(1000),
                        ""EvidenceUrlsJson"" TEXT,
                        ""Status"" INT NOT NULL DEFAULT 0,
                        ""HandlerAdminId"" INT,
                        ""ResolutionNotes"" VARCHAR(1000),
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW(),
                        ""ResolvedAt"" TIMESTAMP
                    );

                    CREATE TABLE IF NOT EXISTS ""AdminScopes"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INT NOT NULL,
                        ""ScopeLevel"" INT NOT NULL,
                        ""ProvinceId"" INT,
                        ""MarketId"" INT,
                        ""AssignedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""AdPackages"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Name"" VARCHAR(150) NOT NULL,
                        ""DurationDays"" INT NOT NULL,
                        ""TargetImpressions"" INT NOT NULL,
                        ""Price"" DECIMAL(18,2) NOT NULL,
                        ""Position"" VARCHAR(50),
                        ""IsActive"" BOOLEAN DEFAULT TRUE
                    );

                    CREATE TABLE IF NOT EXISTS ""AdCampaigns"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""MerchantId"" INT NOT NULL,
                        ""StoreId"" INT NOT NULL,
                        ""ProductId"" INT,
                        ""AdPackageId"" INT NOT NULL,
                        ""TargetProvinceId"" INT,
                        ""TargetMarketId"" INT,
                        ""TargetKeywordsJson"" TEXT,
                        ""StartDate"" TIMESTAMP NOT NULL,
                        ""EndDate"" TIMESTAMP NOT NULL,
                        ""Status"" INT NOT NULL DEFAULT 1,
                        ""Budget"" DECIMAL(18,2) NOT NULL,
                        ""ImpressionsCount"" INT DEFAULT 0,
                        ""ClicksCount"" INT DEFAULT 0,
                        ""ContactClicksCount"" INT DEFAULT 0,
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""AdEventLogs"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""AdCampaignId"" INT NOT NULL,
                        ""EventType"" VARCHAR(30) NOT NULL,
                        ""IpHash"" VARCHAR(64),
                        ""DeviceHash"" VARCHAR(200),
                        ""IsValid"" BOOLEAN DEFAULT TRUE,
                        ""Timestamp"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""ProductMarkets"" (
                        ""MarketId"" INT NOT NULL,
                        ""ProductId"" INT NOT NULL,
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW(),
                        PRIMARY KEY (""MarketId"", ""ProductId"")
                    );

                    CREATE TABLE IF NOT EXISTS ""MobileSellerProfiles"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INT NOT NULL,
                        ""DisplayName"" VARCHAR(150) NOT NULL,
                        ""AvatarUrl"" VARCHAR(500),
                        ""VehicleType"" VARCHAR(100) NOT NULL,
                        ""ItemsDescription"" VARCHAR(500) NOT NULL,
                        ""PrimaryOperatingArea"" VARCHAR(300),
                        ""DefaultRadiusKm"" DOUBLE PRECISION DEFAULT 3.0,
                        ""IsVerified"" BOOLEAN DEFAULT FALSE,
                        ""ReputationScore"" DOUBLE PRECISION DEFAULT 5.0,
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""SellerAvailabilities"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INT NOT NULL,
                        ""IsOnline"" BOOLEAN DEFAULT FALSE,
                        ""CurrentLatitude"" DOUBLE PRECISION NOT NULL,
                        ""CurrentLongitude"" DOUBLE PRECISION NOT NULL,
                        ""ServiceRadiusKm"" DOUBLE PRECISION DEFAULT 3.0,
                        ""LastLocationUpdate"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""LocationSamples"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INT NOT NULL,
                        ""Latitude"" DOUBLE PRECISION NOT NULL,
                        ""Longitude"" DOUBLE PRECISION NOT NULL,
                        ""Timestamp"" TIMESTAMP DEFAULT NOW(),
                        ""ExpiresAt"" TIMESTAMP NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS ""SellerCallRequests"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""RequestCode"" VARCHAR(50) NOT NULL,
                        ""BuyerId"" INT NOT NULL,
                        ""TargetItem"" VARCHAR(150) NOT NULL,
                        ""MeetupLatitude"" DOUBLE PRECISION NOT NULL,
                        ""MeetupLongitude"" DOUBLE PRECISION NOT NULL,
                        ""MeetupAddressNote"" VARCHAR(300),
                        ""BuyerNote"" VARCHAR(500),
                        ""RadiusKm"" DOUBLE PRECISION DEFAULT 3.0,
                        ""Status"" INT NOT NULL DEFAULT 0,
                        ""MatchedSellerId"" INT,
                        ""EstimatedArrivalMinutes"" INT,
                        ""ProtectedContactCode"" VARCHAR(50),
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW(),
                        ""UpdatedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INT,
                        ""UserRole"" VARCHAR(50),
                        ""Action"" VARCHAR(100) NOT NULL,
                        ""EntityName"" VARCHAR(100),
                        ""EntityId"" INT,
                        ""DetailsJson"" TEXT,
                        ""IpHash"" VARCHAR(64),
                        ""Timestamp"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""UserSessions"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""UserId"" INT NOT NULL,
                        ""DeviceName"" VARCHAR(255) NOT NULL,
                        ""IpAddress"" VARCHAR(100),
                        ""Location"" VARCHAR(150),
                        ""IsCurrentSession"" BOOLEAN DEFAULT FALSE,
                        ""IsActive"" BOOLEAN DEFAULT TRUE,
                        ""LoginTime"" TIMESTAMP DEFAULT NOW(),
                        ""LastActiveTime"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""Permissions"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Code"" VARCHAR(100) NOT NULL,
                        ""Name"" VARCHAR(200) NOT NULL,
                        ""Category"" VARCHAR(100) DEFAULT 'General',
                        ""Description"" VARCHAR(500)
                    );

                    CREATE TABLE IF NOT EXISTS ""RolePermissions"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Role"" INT NOT NULL,
                        ""PermissionId"" INT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS ""ContentVersions"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""EntityName"" VARCHAR(50) NOT NULL,
                        ""EntityId"" INT NOT NULL,
                        ""VersionNumber"" INT DEFAULT 1,
                        ""SnapshotJson"" TEXT NOT NULL,
                        ""CreatedByUserId"" INT,
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""ModerationActionHistories"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""CaseId"" INT NOT NULL,
                        ""AdminId"" INT NOT NULL,
                        ""ActionType"" VARCHAR(50) NOT NULL,
                        ""OldStatus"" VARCHAR(50),
                        ""NewStatus"" VARCHAR(50),
                        ""OldDecision"" VARCHAR(50),
                        ""NewDecision"" VARCHAR(50),
                        ""Reason"" VARCHAR(1000) NOT NULL,
                        ""Timestamp"" TIMESTAMP DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS ""ModerationAppeals"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""CaseId"" INT NOT NULL,
                        ""MerchantId"" INT NOT NULL,
                        ""Reason"" VARCHAR(2000) NOT NULL,
                        ""Status"" INT DEFAULT 0,
                        ""AdminResponse"" VARCHAR(1000),
                        ""HandledByAdminId"" INT,
                        ""CreatedAt"" TIMESTAMP DEFAULT NOW(),
                        ""HandledAt"" TIMESTAMP
                    );

                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsMfaEnabled"" BOOLEAN DEFAULT FALSE;
                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""MfaSecretEncrypted"" VARCHAR(500);
                    ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""MfaEnrolledAt"" TIMESTAMP;

                    ALTER TABLE ""ModerationCases"" ADD COLUMN IF NOT EXISTS ""RiskLevel"" INT DEFAULT 0;
                    ALTER TABLE ""ModerationCases"" ADD COLUMN IF NOT EXISTS ""RuleResultsJson"" TEXT;
                    ALTER TABLE ""ModerationCases"" ADD COLUMN IF NOT EXISTS ""CurrentVersionNumber"" INT DEFAULT 1;
                    ALTER TABLE ""ModerationCases"" ADD COLUMN IF NOT EXISTS ""ProvinceId"" INT;
                    ALTER TABLE ""ModerationCases"" ADD COLUMN IF NOT EXISTS ""MarketId"" INT;
                    ALTER TABLE ""ModerationCases"" ADD COLUMN IF NOT EXISTS ""IsEscalated"" BOOLEAN DEFAULT FALSE;
                    ALTER TABLE ""ModerationCases"" ADD COLUMN IF NOT EXISTS ""EscalatedReason"" VARCHAR(1000);
                ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeedData] Schema initialization notice: {ex.Message}");
            }

            // 1. Seed danh mục địa giới: Hà Nội & Các Phường Nội Thành
            if (!await db.Provinces.AnyAsync())
            {
                var hanoi = new Province { Name = "Hà Nội", Code = "HN" };
                db.Provinces.Add(hanoi);
                await db.SaveChangesAsync();

                var dHoanKiem = new District { ProvinceId = hanoi.Id, Name = "Quận Hoàn Kiếm", Code = "HK" };
                var dBaDinh = new District { ProvinceId = hanoi.Id, Name = "Quận Ba Đình", Code = "BD" };
                var dDongDa = new District { ProvinceId = hanoi.Id, Name = "Quận Đống Đa", Code = "DD" };
                var dThanhXuan = new District { ProvinceId = hanoi.Id, Name = "Quận Thanh Xuân", Code = "TX" };
                var dHaiBaTrung = new District { ProvinceId = hanoi.Id, Name = "Quận Hai Bà Trưng", Code = "HBT" };
                var dTayHo = new District { ProvinceId = hanoi.Id, Name = "Quận Tây Hồ", Code = "TH" };
                var dCauGiay = new District { ProvinceId = hanoi.Id, Name = "Quận Cầu Giấy", Code = "CG" };

                db.Districts.AddRange(dHoanKiem, dBaDinh, dDongDa, dThanhXuan, dHaiBaTrung, dTayHo, dCauGiay);
                await db.SaveChangesAsync();

                var wards = new List<Ward>
                {
                    new Ward { DistrictId = dHoanKiem.Id, Name = "Phường Đồng Xuân", Code = "DX" },
                    new Ward { DistrictId = dHoanKiem.Id, Name = "Phường Hàng Bạc", Code = "HB" },
                    new Ward { DistrictId = dBaDinh.Id, Name = "Phường Phúc Xá", Code = "PX" },
                    new Ward { DistrictId = dBaDinh.Id, Name = "Phường Thành Công", Code = "TC" },
                    new Ward { DistrictId = dDongDa.Id, Name = "Phường Văn Miếu", Code = "VM" },
                    new Ward { DistrictId = dThanhXuan.Id, Name = "Phường Nhân Chính", Code = "NC" },
                    new Ward { DistrictId = dHaiBaTrung.Id, Name = "Phường Ngô Thì Nhậm", Code = "NTN" },
                    new Ward { DistrictId = dHaiBaTrung.Id, Name = "Phường Trương Định", Code = "TD" },
                    new Ward { DistrictId = dTayHo.Id, Name = "Phường Bưởi", Code = "BUOI" },
                    new Ward { DistrictId = dCauGiay.Id, Name = "Phường Nghĩa Tân", Code = "NT" }
                };

                db.Wards.AddRange(wards);
                await db.SaveChangesAsync();
            }

            // 2. Seed Target Categories
            var targetCategories = new List<string>
            {
                "Rau củ & Trái cây tươi",
                "Thịt & Gia cầm",
                "Thủy hải sản tươi sống",
                "Thực phẩm chế biến sẵn",
                "Ẩm thực & Chợ ăn vặt",
                "Gia vị & Đồ khô",
                "Gạo, Nếp & Ngũ cốc",
                "Trứng & Sữa",
                "Đồ gia dụng & Tiện ích",
                "Hoa tươi & Đồ thờ cúng"
            };

            foreach (var catName in targetCategories)
            {
                var exists = await db.Categories.AnyAsync(c => c.Name.ToLower() == catName.ToLower());
                if (!exists)
                {
                    db.Categories.Add(new Category { Name = catName });
                }
            }
            await db.SaveChangesAsync();

            // 3. Seed Danh Sách Chợ Nội Thành Hà Nội & Tên Phường Tương Ứng
            var hanoiProv = await db.Provinces.FirstOrDefaultAsync(p => p.Code == "HN");

            if (!await db.Markets.AnyAsync())
            {
                var wDongXuan = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Đồng Xuân");
                var wHangBac = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Hàng Bạc");
                var wPhucXa = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Phúc Xá");
                var wThanhCong = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Thành Công");
                var wVanMieu = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Văn Miếu");
                var wNhanChinh = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Nhân Chính");
                var wNgoThiNham = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Ngô Thì Nhậm");
                var wTruongDinh = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Trương Định");
                var wBuoi = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Bưởi");
                var wNghiaTan = await db.Wards.FirstOrDefaultAsync(w => w.Name == "Phường Nghĩa Tân");

                db.Markets.AddRange(
                    new Market
                    {
                        Name = "Chợ Nhân Chính",
                        Slug = "cho-nhan-chinh",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wNhanChinh?.Id,
                        Address = "Phố Nhân Hòa, Phường Nhân Chính, Quận Thanh Xuân, Hà Nội",
                        Latitude = 21.0040,
                        Longitude = 105.8050,
                        OpeningHours = "05:00 - 18:30",
                        ManagementContact = "Ban Quản lý Chợ Nhân Chính - Tel: 024.3858.1234",
                        PopularCategories = "Rau củ, Thịt heo sạch, Thủy hải sản, Đồ khô, Ẩm thực",
                        IsActive = true
                    },
                    new Market
                    {
                        Name = "Chợ Đồng Xuân",
                        Slug = "cho-dong-xuan",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wDongXuan?.Id,
                        Address = "Phố Đồng Xuân, Phường Đồng Xuân, Quận Hoàn Kiếm, Hà Nội",
                        Latitude = 21.0378,
                        Longitude = 105.8495,
                        OpeningHours = "06:00 - 19:00",
                        ManagementContact = "BQL Chợ Đồng Xuân - Tel: 024.3828.1234",
                        PopularCategories = "Nông sản, Thực phẩm, Gia vị, Đồ khô",
                        IsActive = true
                    },
                    new Market
                    {
                        Name = "Chợ Hàng Bè",
                        Slug = "cho-hang-be",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wHangBac?.Id,
                        Address = "Phố Hàng Bè, Phường Hàng Bạc, Quận Hoàn Kiếm, Hà Nội",
                        Latitude = 21.0322,
                        Longitude = 105.8530,
                        OpeningHours = "06:00 - 18:00",
                        ManagementContact = "BQL Chợ Hàng Bè - Tel: 024.3826.5678",
                        PopularCategories = "Thực phẩm chế biến sẵn, Gà luộc, Đồ thờ cúng",
                        IsActive = true
                    },
                    new Market
                    {
                        Name = "Chợ Long Biên",
                        Slug = "cho-long-bien",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wPhucXa?.Id,
                        Address = "Phố Hồng Hà, Phường Phúc Xá, Quận Ba Đình, Hà Nội",
                        Latitude = 21.0425,
                        Longitude = 105.8528,
                        OpeningHours = "22:00 - 06:00 (Chợ đầu mối đêm)",
                        ManagementContact = "BQL Chợ Đầu Mối Long Biên - Tel: 024.3825.9999",
                        PopularCategories = "Hoa quả tươi sỉ lẻ, Thủy hải sản đầu mối",
                        IsActive = true
                    },
                    new Market
                    {
                        Name = "Chợ Thành Công",
                        Slug = "cho-thanh-cong",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wThanhCong?.Id,
                        Address = "Phố Thành Công, Phường Thành Công, Quận Ba Đình, Hà Nội",
                        Latitude = 21.0195,
                        Longitude = 105.8150,
                        OpeningHours = "05:30 - 19:00",
                        ManagementContact = "BQL Chợ Thành Công - Tel: 024.3831.2222",
                        PopularCategories = "Rau củ hữu cơ, Thịt tươi, Hải sản sống",
                        IsActive = true
                    },
                    new Market
                    {
                        Name = "Chợ Ngô Sĩ Liên",
                        Slug = "cho-ngo-si-lien",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wVanMieu?.Id,
                        Address = "Phố Ngô Sĩ Liên, Phường Văn Miếu, Quận Đống Đa, Hà Nội",
                        Latitude = 21.0265,
                        Longitude = 105.8360,
                        OpeningHours = "05:00 - 18:30",
                        ManagementContact = "BQL Chợ Ngô Sĩ Liên - Tel: 024.3747.8888",
                        PopularCategories = "Thực phẩm tươi sống gia đình",
                        IsActive = true
                    },
                    new Market
                    {
                        Name = "Chợ Hôm - Đức Viên",
                        Slug = "cho-hom",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wNgoThiNham?.Id,
                        Address = "Phố Trần Xuân Soạn, Phường Ngô Thì Nhậm, Quận Hai Bà Trưng, Hà Nội",
                        Latitude = 21.0170,
                        Longitude = 105.8520,
                        OpeningHours = "06:00 - 18:30",
                        ManagementContact = "BQL Chợ Hôm - Tel: 024.3976.3333",
                        PopularCategories = "Nông sản tươi, Đồ khô, Thực phẩm ngon",
                        IsActive = true
                    },
                    new Market
                    {
                        Name = "Chợ Mơ",
                        Slug = "cho-mo",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wTruongDinh?.Id,
                        Address = "459 Bạch Mai, Phường Trương Định, Quận Hai Bà Trưng, Hà Nội",
                        Latitude = 20.9982,
                        Longitude = 105.8523,
                        OpeningHours = "05:00 - 19:30",
                        ManagementContact = "BQL Chợ Mơ - Tel: 024.3863.8888",
                        PopularCategories = "Thịt tươi, Thủy hải sản, Rau củ quả",
                        IsActive = true
                    },
                    new Market
                    {
                        Name = "Chợ Bưởi",
                        Slug = "cho-buoi",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wBuoi?.Id,
                        Address = "Đường Hoàng Hoa Thám, Phường Bưởi, Quận Tây Hồ, Hà Nội",
                        Latitude = 21.0460,
                        Longitude = 105.8080,
                        OpeningHours = "06:00 - 18:00 (Phiên 4, 9 âm lịch)",
                        ManagementContact = "BQL Chợ Bưởi - Tel: 024.3753.1111",
                        PopularCategories = "Cây cảnh, Nông sản, Thực phẩm truyền thống",
                        IsActive = true
                    },
                    new Market
                    {
                        Name = "Chợ Nghĩa Tân",
                        Slug = "cho-nghia-tan",
                        ProvinceId = hanoiProv?.Id,
                        WardId = wNghiaTan?.Id,
                        Address = "Phố Nghĩa Tân, Phường Nghĩa Tân, Quận Cầu Giấy, Hà Nội",
                        Latitude = 21.0460,
                        Longitude = 105.7920,
                        OpeningHours = "05:30 - 19:00",
                        ManagementContact = "BQL Chợ Nghĩa Tân - Tel: 024.3756.4444",
                        PopularCategories = "Ẩm thực chợ ăn vặt, Nông sản, Rau củ",
                        IsActive = true
                    }
                );
                await db.SaveChangesAsync();
            }

            // 4. Seed Users (SuperAdmin, Merchant, Buyer, MobileSeller)
            var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@choviet.vn" || u.Phone == "0900000000");
            if (adminUser == null)
            {
                adminUser = new User
                {
                    Email = "admin@choviet.vn",
                    PasswordHash = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy",
                    Name = "Quản Trị Viên Chợ Việt",
                    Phone = "0900000000",
                    Role = UserRole.SuperAdmin,
                    Address = "Hà Nội",
                    AccessFailedCount = 0,
                    LockoutEnd = null
                };
                db.Users.Add(adminUser);
            }
            else
            {
                adminUser.Email = "admin@choviet.vn";
                adminUser.Phone = "0900000000";
                adminUser.PasswordHash = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";
                adminUser.Role = UserRole.SuperAdmin;
                adminUser.AccessFailedCount = 0;
                adminUser.LockoutEnd = null;
                db.Users.Update(adminUser);
            }

            var defaultUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "user@marketconnect.vn");
            if (defaultUser == null)
            {
                defaultUser = new User
                {
                    Email = "user@marketconnect.vn",
                    PasswordHash = "hashed_password",
                    Name = "Khôi Nguyễn",
                    Phone = "0988 123 456",
                    Role = UserRole.Buyer,
                    Address = "Quận Ba Đình, Hà Nội"
                };
                db.Users.Add(defaultUser);
            }

            var merchantUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "merchant@dongxuan.vn");
            if (merchantUser == null)
            {
                merchantUser = new User
                {
                    Email = "merchant@dongxuan.vn",
                    PasswordHash = "hashed_password",
                    Name = "Bà Tám Nông Sản",
                    Phone = "0912 345 678",
                    Role = UserRole.Merchant,
                    Address = "Gian A12 Chợ Nhân Chính"
                };
                db.Users.Add(merchantUser);
            }

            var streetVendorUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "vendor@street.vn");
            if (streetVendorUser == null)
            {
                streetVendorUser = new User
                {
                    Email = "vendor@street.vn",
                    PasswordHash = "hashed_password",
                    Name = "Chú Ba Bánh Mì",
                    Phone = "0933 999 888",
                    Role = UserRole.MobileSeller,
                    Address = "Khu vực Cầu Giấy, Hà Nội"
                };
                db.Users.Add(streetVendorUser);
            }
            await db.SaveChangesAsync();

            // 5. Seed Merchant Store
            var nhanChinhMarket = await db.Markets.FirstOrDefaultAsync(m => m.Slug == "cho-nhan-chinh") ?? await db.Markets.FirstAsync();
            var produceCat = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Rau củ & Trái cây tươi");

            var sampleStore = await db.Stores.FirstOrDefaultAsync(s => s.StoreName == "Sạp Rau Cô Hoa");
            if (sampleStore == null && nhanChinhMarket != null && produceCat != null)
            {
                sampleStore = new Store
                {
                    UserId = merchantUser.Id,
                    MarketId = nhanChinhMarket.Id,
                    StoreName = "Sạp Rau Cô Hoa",
                    RepresentativeName = "Nguyễn Thị Hoa",
                    VerifiedPhone = "0912345678",
                    StallLocation = "Gian A12 - Chợ Nhân Chính",
                    CategoryId = produceCat.Id,
                    Description = "Chuyên sỉ lẻ hoa quả, rau củ tươi sạch nhập từ vườn trực tiếp hàng ngày.",
                    ContactChannelsJson = "{\"zalo\":\"0912345678\",\"phone\":\"0912345678\"}",
                    PickupMethods = "AtStall,SelfDelivery,AgreedDelivery",
                    Status = StoreStatus.Approved
                };
                db.Stores.Add(sampleStore);
                await db.SaveChangesAsync();
            }

            // 6. Seed Moderation Rules & Ad Packages
            if (!await db.ModerationRules.AnyAsync())
            {
                db.ModerationRules.AddRange(
                    new ModerationRule { RuleKey = "MISSING_REQUIRED", RuleName = "Thiếu trường bắt buộc", Weight = 30, IsActive = true },
                    new ModerationRule { RuleKey = "PROHIBITED_WORDS", RuleName = "Từ ngữ cấm/nhạy cảm", Weight = 50, IsActive = true },
                    new ModerationRule { RuleKey = "PRICE_ANOMALY", RuleName = "Giá bất thường so với danh mục", Weight = 40, IsActive = true },
                    new ModerationRule { RuleKey = "IMAGE_DUPLICATE", RuleName = "Ảnh trùng lặp/Spam", Weight = 35, IsActive = true }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.AdPackages.AnyAsync())
            {
                db.AdPackages.AddRange(
                    new AdPackage { Name = "Gói Ưu Tiên 7 Ngày", DurationDays = 7, TargetImpressions = 5000, Price = 150000, Position = "SearchTop" },
                    new AdPackage { Name = "Gói Gian Hàng Nổi Bật 30 Ngày", DurationDays = 30, TargetImpressions = 30000, Price = 500000, Position = "FeaturedStore" }
                );
                await db.SaveChangesAsync();
            }

            // 7. Seed Mobile Vendor Profile & Availability
            if (!await db.MobileSellerProfiles.AnyAsync() && streetVendorUser != null)
            {
                db.MobileSellerProfiles.Add(new MobileSellerProfile
                {
                    UserId = streetVendorUser.Id,
                    DisplayName = "Chú Ba - Bánh Mì & Nước Mía",
                    VehicleType = "Xe đẩy di động",
                    ItemsDescription = "Bánh mì paté nóng giòn, Nước mía siêu sạch, Xôi mặn",
                    PrimaryOperatingArea = "Cầu Giấy & Xuân Thủy, Hà Nội",
                    DefaultRadiusKm = 3.0,
                    IsVerified = true,
                    ReputationScore = 4.9
                });

                db.SellerAvailabilities.Add(new SellerAvailability
                {
                    UserId = streetVendorUser.Id,
                    IsOnline = true,
                    CurrentLatitude = 21.0365,
                    CurrentLongitude = 105.7830,
                    ServiceRadiusKm = 3.0,
                    LastLocationUpdate = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            // 8. Seed sample products linked to Store
            if (await db.Products.CountAsync() < 6)
            {
                var catProduce = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Rau củ & Trái cây tươi") ?? await db.Categories.FirstAsync();
                var catMeat = await db.Categories.FirstOrDefaultAsync(c => c.Name == "Thịt & Gia cầm") ?? catProduce;

                db.Products.AddRange(
                   new Product 
                   { 
                       Name = "Táo Envy Mỹ Nhập Khẩu Tươi Giòn Ngọt 1kg", 
                       Description = "Táo Envy nhập khẩu trực tiếp từ Mỹ, quả to tròn, thịt giòn ngọt, mọng nước.", 
                       ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=600",
                       Price = 120000,
                       Unit = "kg",
                       PriceType = "Fixed",
                       MinOrderQuantity = 1,
                       StockStatus = "InStock",
                       SearchKeywords = "táo envy, trái cây nhập khẩu, táo Mỹ",
                       ModerationStatus = ModerationStatus.Approved,
                       StoreId = sampleStore?.Id,
                       Address = "Chợ Nhân Chính, Hà Nội",
                       SellerType = "Tiểu thương chợ",
                       CategoryId = catProduce.Id,
                       UserId = merchantUser.Id,
                       CreatedAt = DateTime.Now
                   },
                   new Product 
                   { 
                       Name = "Cam Sành Tiền Giang Mọng Nước Ngọt Thanh 2kg", 
                       Description = "Cam sành vắt nước vỏ mỏng, tép cam vàng tươi mọng nước, vị chua ngọt tự nhiên.", 
                       ImageUrl = "https://images.unsplash.com/photo-1611080626919-7cf5a9dbab5b?w=600",
                       Price = 45000,
                       Unit = "kg",
                       PriceType = "Fixed",
                       MinOrderQuantity = 1,
                       StockStatus = "InStock",
                       SearchKeywords = "cam sành, cam vắt nước, trái cây",
                       ModerationStatus = ModerationStatus.Approved,
                       StoreId = sampleStore?.Id,
                       Address = "Chợ Nhân Chính, Hà Nội",
                       SellerType = "Tiểu thương chợ",
                       CategoryId = catProduce.Id,
                       UserId = merchantUser.Id,
                       CreatedAt = DateTime.Now
                   },
                   new Product 
                   { 
                       Name = "Thịt Bò Mỹ Ba Chỉ Cuộn Lẩu 500g", 
                       Description = "Thịt ba chỉ bò Mỹ cuộn mỏng vừa ăn, mềm mọng thích hợp cho món lẩu.", 
                       ImageUrl = "https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=600",
                       Price = 185000,
                       Unit = "khay 500g",
                       PriceType = "Fixed",
                       MinOrderQuantity = 1,
                       StockStatus = "InStock",
                       SearchKeywords = "thịt bò, ba chỉ bò Mỹ",
                       ModerationStatus = ModerationStatus.Approved,
                       StoreId = sampleStore?.Id,
                       Address = "Chợ Nhân Chính, Hà Nội",
                       SellerType = "Tiểu thương chợ",
                       CategoryId = catMeat.Id,
                       UserId = merchantUser.Id,
                       CreatedAt = DateTime.Now
                   }
                );
                await db.SaveChangesAsync();
            }

            // 9. Seed default System Permissions & Role-Permission Mappings
            if (!await db.Permissions.AnyAsync())
            {
                var permList = new List<Permission>
                {
                    new Permission { Code = "CONTENT_VIEW", Name = "Xem nội dung sản phẩm", Category = "Content" },
                    new Permission { Code = "CONTENT_CREATE", Name = "Tạo mới sản phẩm", Category = "Content" },
                    new Permission { Code = "CONTENT_EDIT", Name = "Sửa sản phẩm", Category = "Content" },
                    new Permission { Code = "CONTENT_APPROVE", Name = "Duyệt sản phẩm", Category = "Content" },
                    new Permission { Code = "CONTENT_REJECT", Name = "Từ chối sản phẩm", Category = "Content" },
                    new Permission { Code = "CONTENT_REQUEST_EDIT", Name = "Yêu cầu chỉnh sửa sản phẩm", Category = "Content" },
                    new Permission { Code = "CONTENT_HIDE", Name = "Ẩn sản phẩm vi phạm", Category = "Content" },
                    new Permission { Code = "CONTENT_ESCALATE", Name = "Chuyển cấp duyệt sản phẩm", Category = "Content" },
                    new Permission { Code = "CONTENT_OVERRIDE", Name = "Ghi đè quyết định duyệt sản phẩm", Category = "Content" },

                    new Permission { Code = "STORE_VIEW", Name = "Xem hồ sơ gian hàng", Category = "Store" },
                    new Permission { Code = "STORE_APPROVE", Name = "Phê duyệt gian hàng", Category = "Store" },
                    new Permission { Code = "STORE_REJECT", Name = "Từ chối gian hàng", Category = "Store" },
                    new Permission { Code = "STORE_SUSPEND", Name = "Tạm ngừng gian hàng", Category = "Store" },
                    new Permission { Code = "STORE_LOCK", Name = "Khóa gian hàng", Category = "Store" },

                    new Permission { Code = "MERCHANT_VIEW", Name = "Xem tiểu thương", Category = "Merchant" },
                    new Permission { Code = "MERCHANT_MANAGE", Name = "Quản lý tiểu thương", Category = "Merchant" },

                    new Permission { Code = "USER_VIEW", Name = "Xem người dùng", Category = "User" },
                    new Permission { Code = "USER_MANAGE", Name = "Quản lý người dùng", Category = "User" },

                    new Permission { Code = "REPORT_VIEW", Name = "Xem báo cáo vi phạm", Category = "Report" },
                    new Permission { Code = "REPORT_MANAGE", Name = "Xử lý báo cáo vi phạm", Category = "Report" },

                    new Permission { Code = "REVIEW_VIEW", Name = "Xem đánh giá", Category = "Review" },
                    new Permission { Code = "REVIEW_MANAGE", Name = "Quản lý đánh giá", Category = "Review" },

                    new Permission { Code = "ADVERTISEMENT_VIEW", Name = "Xem quảng cáo", Category = "Ad" },
                    new Permission { Code = "ADVERTISEMENT_MANAGE", Name = "Quản lý quảng cáo", Category = "Ad" },

                    new Permission { Code = "ANALYTICS_VIEW", Name = "Xem báo cáo thống kê", Category = "Analytics" },

                    new Permission { Code = "ROLE_VIEW", Name = "Xem phân quyền", Category = "Role" },
                    new Permission { Code = "ROLE_ASSIGN", Name = "Cấp quyền", Category = "Role" },
                    new Permission { Code = "ROLE_REVOKE", Name = "Thu hồi quyền", Category = "Role" },

                    new Permission { Code = "AUDIT_VIEW", Name = "Xem nhật ký hệ thống (Audit)", Category = "System" },
                    new Permission { Code = "MODERATION_RULE_VIEW", Name = "Xem quy tắc kiểm duyệt", Category = "System" },
                    new Permission { Code = "MODERATION_RULE_MANAGE", Name = "Quản lý quy tắc kiểm duyệt", Category = "System" }
                };

                db.Permissions.AddRange(permList);
                await db.SaveChangesAsync();

                var allPerms = await db.Permissions.ToListAsync();
                var rolePerms = new List<RolePermission>();

                // SuperAdmin: ALL
                foreach (var p in allPerms)
                {
                    rolePerms.Add(new RolePermission { Role = UserRole.SuperAdmin, PermissionId = p.Id });
                }

                // ProvinceAdmin
                foreach (var p in allPerms.Where(x => x.Category != "Ad"))
                {
                    rolePerms.Add(new RolePermission { Role = UserRole.ProvinceAdmin, PermissionId = p.Id });
                }

                // MarketAdmin
                foreach (var p in allPerms.Where(x => x.Category == "Content" || x.Category == "Store" || x.Category == "Merchant" || x.Category == "Review"))
                {
                    rolePerms.Add(new RolePermission { Role = UserRole.MarketAdmin, PermissionId = p.Id });
                }

                // Moderator
                foreach (var p in allPerms.Where(x => x.Code.StartsWith("CONTENT_") || x.Code.StartsWith("STORE_") || x.Code.StartsWith("REVIEW_")))
                {
                    rolePerms.Add(new RolePermission { Role = UserRole.Moderator, PermissionId = p.Id });
                }

                // AdStaff
                foreach (var p in allPerms.Where(x => x.Category == "Ad" || x.Code == "ANALYTICS_VIEW"))
                {
                    rolePerms.Add(new RolePermission { Role = UserRole.AdStaff, PermissionId = p.Id });
                }

                // SupportStaff
                foreach (var p in allPerms.Where(x => x.Category == "User" || x.Category == "Report" || x.Code == "MERCHANT_VIEW"))
                {
                    rolePerms.Add(new RolePermission { Role = UserRole.SupportStaff, PermissionId = p.Id });
                }

                db.RolePermissions.AddRange(rolePerms);
                await db.SaveChangesAsync();
            }
        }
    }
}
