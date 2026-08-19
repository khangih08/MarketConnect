using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Services
{
    public class ModerationWorkflowGuard : IModerationWorkflowGuard
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminMfaService _mfaService;
        private readonly ApplicationDbContext _db;

        public ModerationWorkflowGuard(ICurrentUserService currentUser, IAdminMfaService mfaService, ApplicationDbContext db)
        {
            _currentUser = currentUser;
            _mfaService = mfaService;
            _db = db;
        }

        public async Task<WorkflowGuardResult> ValidateWorkflowStepAsync(
            string permissionCode,
            int? targetMarketId,
            int? targetProvinceId,
            ModerationStatus? currentStatus = null,
            ModerationStatus? targetStatus = null)
        {
            // 1. Authentication Check
            if (_currentUser.UserId <= 0)
            {
                return new WorkflowGuardResult
                {
                    IsAllowed = false,
                    StatusCode = 401,
                    ErrorMessage = "Vui lòng đăng nhập tài khoản quản trị để thực hiện thao tác này."
                };
            }

            var role = _currentUser.Role;

            // 2. Admin MFA Check
            if (_mfaService.IsMfaRequiredForRole(role) && !_currentUser.IsMfaVerified)
            {
                return new WorkflowGuardResult
                {
                    IsAllowed = false,
                    StatusCode = 403,
                    ErrorMessage = "Yêu cầu xác thực MFA (Mã OTP Quản trị) trước khi thực hiện thao tác kiểm duyệt."
                };
            }

            // 3. Permission Check
            if (role != UserRole.SuperAdmin)
            {
                bool hasPermission = await _db.RolePermissions
                    .Include(rp => rp.Permission)
                    .AnyAsync(rp => rp.Role == role && rp.Permission!.Code == permissionCode);

                if (!hasPermission)
                {
                    return new WorkflowGuardResult
                    {
                        IsAllowed = false,
                        StatusCode = 403,
                        ErrorMessage = $"Tài khoản vai trò '{role}' không có quyền '{permissionCode}'."
                    };
                }
            }

            // 4. Data Scope Check
            if (role != UserRole.SuperAdmin)
            {
                var scopes = _currentUser.AdminScopes;

                if (role == UserRole.ProvinceAdmin && targetProvinceId.HasValue)
                {
                    bool inScope = scopes.Any(s => s.ProvinceId == targetProvinceId.Value || s.ScopeLevel == ScopeLevel.System);
                    if (!inScope)
                    {
                        return new WorkflowGuardResult
                        {
                            IsAllowed = false,
                            StatusCode = 403,
                            ErrorMessage = "Tài khoản Quản trị tỉnh không có thẩm quyền kiểm duyệt dữ liệu thuộc Tỉnh/Thành phố này."
                        };
                    }
                }
                else if ((role == UserRole.MarketAdmin || role == UserRole.Moderator) && targetMarketId.HasValue)
                {
                    bool inScope = scopes.Any(s => s.MarketId == targetMarketId.Value || s.ScopeLevel == ScopeLevel.System);
                    if (!inScope)
                    {
                        return new WorkflowGuardResult
                        {
                            IsAllowed = false,
                            StatusCode = 403,
                            ErrorMessage = "Tài khoản Quản trị chợ/Kiểm duyệt viên không có thẩm quyền kiểm duyệt dữ liệu thuộc Chợ này."
                        };
                    }
                }
            }

            // 5. State Transition Check
            if (currentStatus.HasValue && targetStatus.HasValue)
            {
                if (!IsValidStateTransition(currentStatus.Value, targetStatus.Value))
                {
                    return new WorkflowGuardResult
                    {
                        IsAllowed = false,
                        StatusCode = 400,
                        ErrorMessage = $"Chuyển đổi trạng thái từ '{currentStatus.Value}' sang '{targetStatus.Value}' không hợp lệ."
                    };
                }
            }

            return new WorkflowGuardResult { IsAllowed = true, StatusCode = 200 };
        }

        public bool IsValidStateTransition(ModerationStatus currentStatus, ModerationStatus targetStatus)
        {
            if (currentStatus == targetStatus) return true;

            return currentStatus switch
            {
                ModerationStatus.Draft => targetStatus == ModerationStatus.PendingAutoReview,
                ModerationStatus.PendingAutoReview => targetStatus == ModerationStatus.Approved ||
                                                      targetStatus == ModerationStatus.PendingManualReview ||
                                                      targetStatus == ModerationStatus.ChangesRequired ||
                                                      targetStatus == ModerationStatus.Rejected,
                ModerationStatus.PendingManualReview => targetStatus == ModerationStatus.Approved ||
                                                        targetStatus == ModerationStatus.ChangesRequired ||
                                                        targetStatus == ModerationStatus.Rejected ||
                                                        targetStatus == ModerationStatus.Suspended ||
                                                        targetStatus == ModerationStatus.Archived,
                ModerationStatus.ChangesRequired => targetStatus == ModerationStatus.PendingAutoReview ||
                                                     targetStatus == ModerationStatus.PendingManualReview,
                ModerationStatus.Approved => targetStatus == ModerationStatus.ChangesRequired ||
                                             targetStatus == ModerationStatus.Rejected ||
                                             targetStatus == ModerationStatus.Suspended ||
                                             targetStatus == ModerationStatus.Archived,
                ModerationStatus.Rejected => targetStatus == ModerationStatus.Approved ||
                                             targetStatus == ModerationStatus.PendingManualReview ||
                                             targetStatus == ModerationStatus.Draft,
                ModerationStatus.Suspended => targetStatus == ModerationStatus.Approved ||
                                              targetStatus == ModerationStatus.Rejected,
                _ => false
            };
        }
    }
}
