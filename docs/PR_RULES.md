# Quy tắc Pull Request

## Mô hình nhánh

- `main`: nhánh gốc của MarketConnect, không commit trực tiếp phục vụ đề tài.
- `feature/recommendation-thesis`: **nhánh tích hợp** của đề tài hệ thống gợi ý; mọi thay đổi đề tài được merge vào đây qua PR.
- Nhánh làm việc tách từ nhánh tích hợp, đặt tên:
  - `feat/<mo-ta-ngan>` — tính năng mới
  - `fix/<mo-ta-ngan>` — sửa lỗi
  - `docs/<mo-ta-ngan>` — tài liệu, nhật ký tuần
  - `exp/<mo-ta-ngan>` — thử nghiệm mô hình/dữ liệu
  - `chore/<mo-ta-ngan>` — cấu hình, build, dependency

## Commit

- Theo Conventional Commits: `feat: ...`, `fix: ...`, `docs: ...`, `test: ...`, `refactor: ...`, `chore: ...`.
- Mỗi commit một thay đổi logic; không commit file build (`bin/`, `obj/`, `node_modules/`, `.vs/`) hay bí mật (mật khẩu, khóa API).

## Điều kiện mở PR

1. Base branch là `feature/recommendation-thesis` (không mở PR thẳng vào `main`).
2. Rebase/merge nhánh tích hợp mới nhất, không còn xung đột.
3. `dotnet build MarketConnect.slnx` thành công.
4. `dotnet test` pass (hoặc ghi rõ test lỗi có sẵn từ trước).
5. Có migration EF Core nếu thay đổi model dữ liệu.
6. Cập nhật `docs/progress/weekXX.md` nếu PR hoàn thành một mục trong checklist tuần.
7. Điền đầy đủ PR template.

## Review và merge

- PR nhỏ (ưu tiên < 400 dòng thay đổi).
- Tối thiểu 1 người review approve (hoặc tự review theo checklist nếu làm một mình, ghi rõ trong PR).
- Dùng **Squash and merge**; tiêu đề squash theo Conventional Commits.
- Xóa nhánh làm việc sau khi merge.
- Chỉ merge `feature/recommendation-thesis` → `main` khi kết thúc mốc và đã được duyệt.

## Bảo vệ nhánh (cấu hình trên GitHub)

- `feature/recommendation-thesis`: yêu cầu PR, cấm force-push, cấm xóa nhánh.

