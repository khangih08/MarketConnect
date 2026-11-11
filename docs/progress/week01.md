# Nhật ký tuần 01

## Mốc mã nguồn ban đầu

- Ngày kiểm tra: 11/11/2026
- Người thực hiện: Khang
- Repository: https://github.com/khangih08/MarketConnect
- Nhánh ban đầu: main
- SHA gốc: 1ac39a1e6c4057d3e67a56f85e49d51e63ae3f7a
- Commit gần nhất: 2026-08-24 11:25:08 +0700 — Khang Nguyen — "update .."
- Trạng thái ban đầu: working tree clean (nhánh `main` đồng bộ với `origin/main`)
- Nhánh tích hợp dự kiến: feature/recommendation-thesis

### Ghi chú

- SHA trên được lấy trực tiếp từ repository trên máy.
- Chưa kết luận mã nguồn hiện tại trùng với phiên bản trong roadmap.
- Lệnh đã dùng: `git branch --show-current`, `git rev-parse HEAD`, `git log -1`, `git status`.

## Môi trường máy phát triển

| Hạng mục | Giá trị thực tế |
|---|---|
| Tên máy | DESKTOP-PRHBH94 |
| Hệ điều hành | Microsoft Windows 10 Home Single Language, 10.0.19045, 64-bit |
| CPU | Intel(R) Core(TM) i7-8565U CPU @ 1.80GHz |
| RAM | 15.8 GB |
| Shell | Windows PowerShell |

## Phiên bản công cụ (kết quả lệnh thực tế)

| Công cụ | Lệnh | Kết quả |
|---|---|---|
| .NET Host | `dotnet --info` | 10.0.11 (x64, commit e2f47b0110) |
| .NET SDK | `dotnet --info` / `dotnet --list-sdks` | Ban đầu: **No SDKs were found** → 10:17: **10.0.404** |
| .NET runtimes | `dotnet --info` | Microsoft.NETCore.App 6.0.28, 8.0.11, 10.0.11; WindowsDesktop.App 6.0.28, 8.0.11, 10.0.11 |
| global.json | `dotnet --info` | Not found |
| Docker | `docker --version` | Docker version 29.8.2, build 7fc2dff |
| Docker daemon | `docker info` | Đang chạy (Server 29.8.2) |
| Docker Compose | `docker compose version` | v5.6.0 |
| PostgreSQL client | `psql --version` | **Không tìm thấy lệnh `psql`** |
| Git | `git --version` | 2.45.1.windows.1 |
| Node.js | `node --version` | v24.20.0 |
| npm | `npm --version` | 11.19.0 |
| dotnet-ef | `dotnet ef --version` / `dotnet tool list -g` | Ban đầu: **Không tìm thấy** → 10:31: **11.0.0** (global tool, `C:\Users\Tuan Manh\.dotnet\tools\dotnet-ef.exe`) |

## Yêu cầu của dự án (đọc từ mã nguồn)

- Tất cả project `.csproj` dùng `TargetFramework` = `net10.0` → cần **.NET SDK 10.x**.
- `docker-compose.yml` chỉ khai báo service `db` (image `postgres:15`, cổng 5432, DB `MarketConnectDb`).
- `appsettings.json` kết nối PostgreSQL `Host=localhost;Port=5432;Database=MarketConnectDb`.
- `Program.cs` tạo `ElasticsearchClient` với `Elasticsearch:Url` (mặc định `http://localhost:9200`) — **chưa có service Elasticsearch trong docker-compose**.
- `MultiMarketProductService` nhận `IConnectionMultiplexer?` (Redis) dạng tùy chọn — hiện chưa đăng ký trong `Program.cs`.
- `ClientApp/node_modules` chưa tồn tại (chưa chạy `npm install`).

## Danh sách dependency còn thiếu

| # | Dependency | Mức độ | Lý do / bằng chứng | Hướng xử lý |
|---|---|---|---|---|
| 1 | .NET SDK 10.x (✅ đã cài 10.0.404) | ~~Chặn~~ | `dotnet --info`: "No SDKs were found"; dự án target `net10.0` | Cài .NET 10 SDK (https://dotnet.microsoft.com/download), kiểm tra lại `dotnet --list-sdks` |
| 2 | PostgreSQL client (`psql`) | Trung bình | `psql` không nhận diện | Cài PostgreSQL client tools, hoặc dùng `docker exec -it <container> psql -U postgres` |
| 3 | dotnet-ef (✅ đã cài 11.0.0) | ~~Trung bình~~ | Ban đầu không có lệnh `dotnet-ef` (cần cho migration) | Đã cài global tool; `dotnet ef migrations list` chạy được |
| 4 | Elasticsearch | Trung bình | `Program.cs` dùng `http://localhost:9200`, không có trong docker-compose | Bổ sung service Elasticsearch vào compose hoặc cấu hình `Elasticsearch:Url` |
| 5 | Gói npm của ClientApp | Thấp | Chưa có `ClientApp/node_modules` | `cd ClientApp; npm install` |
| 6 | Redis | Thấp (tùy chọn) | Tham chiếu `StackExchange.Redis`, chưa đăng ký DI | Chỉ cần khi bật cache |

> Lưu ý thêm: `MarketConnect.Tests` tham chiếu `Microsoft.EntityFrameworkCore.InMemory` 10.0.0-preview trong khi các project khác dùng EF Core 8.0.4 — cần kiểm tra khi build được.

## Nhánh tích hợp và quy tắc PR

- Nhánh tích hợp: `feature/recommendation-thesis`, tạo từ SHA gốc `1ac39a1e6c4057d3e67a56f85e49d51e63ae3f7a`.
- Quy tắc PR: xem [docs/PR_RULES.md](../PR_RULES.md); template: `.github/pull_request_template.md`.

## Checklist tuần 01

- [x] Ghi SHA gốc, nhánh, commit gần nhất, trạng thái working tree
- [x] Tạo nhánh tích hợp `feature/recommendation-thesis`
- [x] Viết quy tắc PR và PR template
- [x] Chạy `dotnet --info`, `docker --version`, kiểm tra `psql`
- [x] Ghi thông tin máy / OS / phiên bản công cụ
- [x] Lập danh sách dependency còn thiếu
- [x] Cài .NET SDK 10.x — `dotnet --list-sdks`: 10.0.404 (kiểm tra lại lúc 10:17 ngày 11/11/2026)
- [x] Dùng `psql` trong container (`docker exec marketconnect-db-1 psql ...`); chưa cài `psql` trên máy
- [x] Cài `dotnet-ef` — 11.0.0, `dotnet ef migrations list` chạy được (4 migration, đều Pending)
- [ ] Áp dụng migration vào DB (`dotnet ef database update`) — chưa chạy
- [x] Khởi động PostgreSQL bằng `docker compose up -d db` và kết nối thử — PostgreSQL 15.19, DB `MarketConnectDb` chưa có bảng nào
- [ ] Bổ sung/cấu hình Elasticsearch
- [x] `dotnet build MarketConnect.slnx` thành công — 0 lỗi, 4 cảnh báo
- [x] `dotnet test` chạy được — 4 pass / 0 fail / 0 skip
- [ ] `npm install` cho ClientApp
- [x] Push nhánh tích hợp lên remote — `git push -u origin feature/recommendation-thesis` thành công (11/11/2026)
- [ ] Bật branch protection cho `feature/recommendation-thesis` trên GitHub
- [ ] Gỡ `bin/` và `obj/` khỏi git (đang bị track, thay đổi sau mỗi lần build) và thêm `.gitignore`
- [ ] Thêm `MarketConnect.Tests` vào `MarketConnect.slnx`
- [ ] Nâng cấp `Microsoft.Extensions.Caching.Memory` (cảnh báo NU1903)

## Kết quả build và test (11/11/2026)

### `dotnet build MarketConnect.slnx`

- SDK: 10.0.404
- Kết quả: **Build succeeded**, 0 Error(s), 4 Warning(s), thời gian 00:00:05.52
- Project build được: MarketConnect.Data, MarketConnect.Services, MarketConnect (đều `net10.0`)
- Cảnh báo: `NU1903` — gói `Microsoft.Extensions.Caching.Memory` 8.0.0 có lỗ hổng mức cao (GHSA-qj66-m88j-hmgj), xuất hiện ở MarketConnect.Data và MarketConnect.Services → cần nâng cấp phiên bản gói.

### `dotnet test`

- `MarketConnect.slnx` **không chứa** project `MarketConnect.Tests`, nên chạy trực tiếp:
  `dotnet test MarketConnect.Tests/MarketConnect.Tests.csproj`
- Kết quả: **Passed!** — Failed: 0, Passed: 4, Skipped: 0, Total: 4, Duration: 3 s (`MarketConnect.Tests.dll`, net10.0)
- Đề xuất: thêm `MarketConnect.Tests` vào `MarketConnect.slnx` để `dotnet test MarketConnect.slnx` chạy được test.

### PostgreSQL

- `docker compose ps`: `marketconnect-db-1` (postgres:15) đang chạy, cổng 5432
- `select version();` → PostgreSQL 15.19 (Debian 15.19-1.pgdg13+2)
- `\dt` → "Did not find any relations" (chưa chạy migration)
- Cảnh báo compose: thuộc tính `version` trong `docker-compose.yml` đã lỗi thời.

### EF Core CLI (`dotnet ef`) — kiểm tra lúc 10:31 ngày 11/11/2026

- `dotnet ef --version` → Entity Framework Core .NET Command-line Tools **11.0.0**
- `dotnet tool list -g` → `dotnet-ef 11.0.0`
- Lệnh: `dotnet ef migrations list --project MarketConnect.Data --startup-project MarketConnect`
  - Build succeeded, kết nối được PostgreSQL (kiểm tra bảng `__EFMigrationsHistory`)
  - Kết quả:
    - `20260722031415_InitialDockerDb` (Pending)
    - `20260723081510_AddOtpVerification` (Pending)
    - `20260728101445_AddProfileFieldsToUser` (Pending)
    - `20260802134049_UpdateProductDirectListing` (Pending)
- Lưu ý: tool 11.0.0 mới hơn EF Core runtime trong dự án (8.0.4). Lệnh `migrations list` hoạt động bình thường; nếu gặp lỗi khi `database update`/`migrations add` thì cân nhắc cài tool cùng phiên bản: `dotnet tool update -g dotnet-ef --version 8.0.4`.
- Chưa chạy `dotnet ef database update` (DB vẫn trống).

## Lịch sử thay đổi trong tuần 01

| Thời điểm (11/11/2026) | Thay đổi |
|---|---|
| 09:58 | Ghi mốc mã nguồn: nhánh `main`, SHA `1ac39a1e6c4057d3e67a56f85e49d51e63ae3f7a`, working tree clean |
| 10:02 | Tạo nhánh `feature/recommendation-thesis` từ SHA gốc; commit `523b365` (nhật ký ban đầu) |
| 10:03 | Kiểm tra môi trường: chưa có .NET SDK, chưa có `psql`, chưa có `dotnet-ef`; Docker 29.8.2 chạy được |
| 10:04 | Thêm `docs/PR_RULES.md`, `.github/pull_request_template.md`; commit `4f41b26` |
| 10:17 | .NET SDK 10.0.404 đã được cài; PostgreSQL 15.19 chạy trong container `marketconnect-db-1` |
| 10:17 | `dotnet build MarketConnect.slnx`: thành công (0 lỗi, 4 cảnh báo NU1903) |
| 10:18 | `dotnet test MarketConnect.Tests`: 4 pass / 0 fail; commit `0313e7b` |
| 10:18 | Push `feature/recommendation-thesis` lên `origin` |
| 10:19 | Bật branch protection thất bại (công cụ trình duyệt không kết nối được Chrome) — chưa có thay đổi trên GitHub; commit `762939a` |
| 10:31 | `dotnet-ef` 11.0.0 đã được cài; `dotnet ef migrations list` chạy được, 4 migration Pending |
