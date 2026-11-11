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
| .NET SDK | `dotnet --info` | **No SDKs were found** |
| .NET runtimes | `dotnet --info` | Microsoft.NETCore.App 6.0.28, 8.0.11, 10.0.11; WindowsDesktop.App 6.0.28, 8.0.11, 10.0.11 |
| global.json | `dotnet --info` | Not found |
| Docker | `docker --version` | Docker version 29.8.2, build 7fc2dff |
| Docker daemon | `docker info` | Đang chạy (Server 29.8.2) |
| Docker Compose | `docker compose version` | v5.6.0 |
| PostgreSQL client | `psql --version` | **Không tìm thấy lệnh `psql`** |
| Git | `git --version` | 2.45.1.windows.1 |
| Node.js | `node --version` | v24.20.0 |
| npm | `npm --version` | 11.19.0 |
| dotnet-ef | `Get-Command dotnet-ef` | **Không tìm thấy** |

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
| 1 | .NET SDK 10.x | **Chặn** | `dotnet --info`: "No SDKs were found"; dự án target `net10.0` | Cài .NET 10 SDK (https://dotnet.microsoft.com/download), kiểm tra lại `dotnet --list-sdks` |
| 2 | PostgreSQL client (`psql`) | Trung bình | `psql` không nhận diện | Cài PostgreSQL client tools, hoặc dùng `docker exec -it <container> psql -U postgres` |
| 3 | dotnet-ef | Trung bình | Không có lệnh `dotnet-ef` (cần cho migration) | Sau khi có SDK: `dotnet tool install --global dotnet-ef` |
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
- [ ] Cài .NET SDK 10.x
- [ ] Cài PostgreSQL client (`psql`) hoặc thống nhất dùng `psql` trong container
- [ ] Cài `dotnet-ef`
- [ ] Khởi động PostgreSQL bằng `docker compose up -d db` và kết nối thử
- [ ] Bổ sung/cấu hình Elasticsearch
- [ ] `dotnet restore` + `dotnet build MarketConnect.slnx` thành công
- [ ] `dotnet test` chạy được (ghi số test pass/fail)
- [ ] `npm install` cho ClientApp
- [ ] Push nhánh tích hợp lên remote và bật branch protection
