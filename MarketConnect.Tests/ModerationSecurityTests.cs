using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MarketConnect.Tests
{
    public class ModerationSecurityTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var db = new ApplicationDbContext(options);

            // Seed Permissions
            db.Permissions.Add(new Permission { Id = 1, Code = "CONTENT_APPROVE", Name = "Duyệt sản phẩm", Category = "Content" });
            db.Permissions.Add(new Permission { Id = 2, Code = "STORE_LOCK", Name = "Khóa gian hàng", Category = "Store" });
            db.Permissions.Add(new Permission { Id = 3, Code = "ROLE_ASSIGN", Name = "Cấp quyền", Category = "Role" });

            // Seed Role Permissions
            db.RolePermissions.Add(new RolePermission { Id = 1, Role = UserRole.Moderator, PermissionId = 1 });
            db.RolePermissions.Add(new RolePermission { Id = 2, Role = UserRole.MarketAdmin, PermissionId = 1 });

            db.SaveChanges();
            return db;
        }

        [Fact]
        public async Task RolePermission_ModeratorCannotLockStore()
        {
            var db = GetInMemoryDbContext();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.IsAuthenticated).Returns(true);
            mockUser.Setup(u => u.UserId).Returns(10);
            mockUser.Setup(u => u.Role).Returns(UserRole.Moderator);
            mockUser.Setup(u => u.IsMfaVerified).Returns(true);
            mockUser.Setup(u => u.AdminScopes).Returns(new List<AdminScope>());

            var mockMfa = new Mock<IAdminMfaService>();
            mockMfa.Setup(m => m.IsMfaRequiredForRole(UserRole.Moderator)).Returns(false);

            var guard = new ModerationWorkflowGuard(mockUser.Object, mockMfa.Object, db);

            var result = await guard.ValidateWorkflowStepAsync("STORE_LOCK", null, null);

            Assert.False(result.IsAllowed);
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("không có quyền 'STORE_LOCK'", result.ErrorMessage);
        }

        [Fact]
        public async Task DataScope_MarketAdminBlockedFromOtherMarket()
        {
            var db = GetInMemoryDbContext();
            var mockUser = new Mock<ICurrentUserService>();
            mockUser.Setup(u => u.IsAuthenticated).Returns(true);
            mockUser.Setup(u => u.UserId).Returns(20);
            mockUser.Setup(u => u.Role).Returns(UserRole.MarketAdmin);
            mockUser.Setup(u => u.IsMfaVerified).Returns(true);
            mockUser.Setup(u => u.AdminScopes).Returns(new List<AdminScope>
            {
                new AdminScope { UserId = 20, ScopeLevel = ScopeLevel.Market, MarketId = 1 }
            });

            var mockMfa = new Mock<IAdminMfaService>();
            mockMfa.Setup(m => m.IsMfaRequiredForRole(UserRole.MarketAdmin)).Returns(false);

            var guard = new ModerationWorkflowGuard(mockUser.Object, mockMfa.Object, db);

            // Attempt action on Market #2 (User is in Market #1)
            var result = await guard.ValidateWorkflowStepAsync("CONTENT_APPROVE", targetMarketId: 2, targetProvinceId: null);

            Assert.False(result.IsAllowed);
            Assert.Equal(403, result.StatusCode);
            Assert.Contains("không có thẩm quyền", result.ErrorMessage);
        }

        [Fact]
        public void StateTransition_InvalidJumpThrows()
        {
            var mockUser = new Mock<ICurrentUserService>();
            var mockMfa = new Mock<IAdminMfaService>();
            var db = GetInMemoryDbContext();

            var guard = new ModerationWorkflowGuard(mockUser.Object, mockMfa.Object, db);

            // Draft cannot jump directly to Approved
            bool isValid = guard.IsValidStateTransition(ModerationStatus.Draft, ModerationStatus.Approved);

            Assert.False(isValid);
        }

        [Fact]
        public async Task AdminMfa_EncryptionAndValidation()
        {
            var db = GetInMemoryDbContext();
            var mfaService = new AdminMfaService(db);

            string secret = "SECRET1234567890";
            string encrypted = mfaService.EncryptSecret(secret);
            string decrypted = mfaService.DecryptSecret(encrypted);

            Assert.NotEqual(secret, encrypted);
            Assert.Equal(secret, decrypted);

            bool isValid = await mfaService.ValidateAdminMfaPasscodeAsync(1, "123456");
            Assert.True(isValid);
        }
    }
}
