# CI — cổng chất lượng trên pull request

`.github/workflows/test.yml`. Bốn job trên `ubuntu-latest`, .NET 10.

Đây **không phải** đường triển khai. Sản phẩm vẫn deploy bằng **Jenkins lên 6 máy IIS** (`AD-21`).
Workflow này chỉ trả lời đúng một câu hỏi: *thay đổi này có phá cách ly tenant không?*

---

## Bốn job

| Job | Chạy khi nào | Chặn cái gì |
|---|---|---|
| `lint` | mọi push/PR | `dotnet format --verify-no-changes` — định dạng lệch `.editorconfig` |
| `tenant-leak-tests` | mọi push/PR | Bộ test rò rỉ hai tenant (`AD-24`) + cổng đếm bài test |
| `burn-in` | PR, lịch tuần, chạy tay | Test chập chờn — lặp bộ test 5 lần, đỏ ngay vòng đầu tiên hỏng |
| `report` | luôn luôn | Không chặn gì; ghi bảng kết quả vào job summary |

Lịch: `cron "0 2 * * 0"` — 02:00 UTC Chủ nhật (09:00 giờ Việt Nam).

---

## Cổng đếm bài test — thứ đáng chú ý nhất

Bước `Assert test count ratchet` đọc file TRX và **fail** nếu:

- `failed != 0`
- `passed != total` — có bài bị `Skip` hoặc không chạy
- `total < MINIMUM_TENANT_LEAK_TESTS` (hiện là **45**, khai ở `env` cấp workflow)

Vì sao cần: một bộ test lặng lẽ ngừng chạy phần lớn chính nó — vì một bộ lọc đặt sai, một
`[Fact(Skip = "tạm")]` quên gỡ — vẫn cho ra CI **xanh**, và còn xanh *nhanh hơn*. Với một bộ test có
nhiệm vụ chứng minh không có rò rỉ dữ liệu giữa 18 khách hàng, đó là kiểu hỏng tệ nhất: nó im lặng.

**Thêm bài test mới thì nâng con số đó lên.** `AD-24` bắt mở rộng bộ test mỗi khi thêm một cơ chế cách
ly mới; con số này là chỗ ghi lại cam kết ấy.

---

## Ba quyết định đi ngược khuyến nghị mặc định, có lý do

**1. Có burn-in, dù đây là backend.**
Hướng dẫn mặc định của Test Architect: backend thì bỏ burn-in, vì test backend tất định còn chập chờn
là chuyện của UI. Ở đây vẫn bật, vì bộ test này **đã chập chờn một lần thật**: các lớp test chạy song
song dùng chung một ô `AsyncLocal` tĩnh. Đã xử lý bằng `CollectionBehavior(DisableTestParallelization = true)`,
nhưng một bộ test rò rỉ mà thỉnh thoảng đỏ vì tự nhiễu thì lần đỏ **thật** sẽ bị cho là "lại flaky".
Burn-in là thứ giữ cho màu đỏ còn đáng tin. Năm vòng × ~10 giây, gần như miễn phí.

**2. Không tự thử lại (retry) khi test đỏ.**
Checklist mặc định khuyên cấu hình retry 2–3 lần cho lỗi thoáng qua. Ở đây **cố ý không**: retry trên
một bộ test cách ly che đi đúng thứ burn-in sinh ra để phơi bày. Đỏ là đỏ.

**3. Không chia shard.**
45 bài chạy ~9 giây. Chia 4 shard tốn 4 lần khởi động runner và 4 lần restore NuGet để tiết kiệm vài
giây. Xem lại khi bộ test vượt **5 phút**.

---

## Bí mật cần cấu hình: không có

Pipeline này không dùng secret nào. Không có Pact broker, không có webhook Slack, không có registry.
`GITHUB_TOKEN` mặc định là đủ, và workflow đã khai `permissions: contents: read`.

Nếu sau này muốn báo lỗi qua Slack thì thêm secret **trước**, rồi mới thêm step — một step trỏ vào
secret chưa tồn tại sẽ làm đỏ mọi lần chạy.

---

## Vì sao mọi lệnh có `-m:1 -nr:false`

Máy build gặp trục trặc MSBuild node reuse; thiếu hai cờ này thì `dotnet test` hỏng thất thường. Trên
runner sạch của GitHub thì vô hại, nên giữ cho lệnh ở CI và ở máy lập trình viên giống hệt nhau.

---

## Khi CI đỏ thì làm gì

| Job đỏ | Chạy tại máy | Nghĩa là gì |
|---|---|---|
| `lint` | `dotnet format Staxi.slnx` rồi commit | Định dạng lệch `.editorconfig` |
| `tenant-leak-tests` | `dotnet test Staxi.slnx -m:1 -nr:false` | **Đọc tên bài test trước khi sửa gì.** Mỗi bài ứng với một AD; xem `tests/Staxi.TenantLeakTests/README.md` |
| `tenant-leak-tests` ở bước ratchet | xem log TRX in ra | Có bài bị bỏ qua, hoặc ai đó xoá bài test mà quên hạ ngưỡng |
| `burn-in` | chạy `dotnet test` 5 lần liên tiếp | Có bài chập chờn — **đừng** thêm retry, tìm trạng thái dùng chung |

Tải artifact `tenant-leak-test-results` (giữ 30 ngày) để xem TRX của lần chạy đỏ.

---

## Chưa có, biết trước khi cần

- **Chưa có độ phủ (coverage).** Cần thêm `coverlet.collector` vào project test rồi mới bật
  `--collect:"XPlat Code Coverage"`.
- **Chưa có `global.json`.** SDK được ghim bằng `actions/setup-dotnet` ở CI, nhưng máy lập trình viên
  thì không có gì ghim. Thêm `global.json` khi đội đông lên.
- **Chưa có job cho frontend.** Khi `apps/admin-web` (Vite + React) xuất hiện, stack thành `fullstack`
  và cần thêm một nhánh job riêng.
- **Chưa chạy lần nào.** Toàn bộ code nền tảng hiện vẫn chưa commit; workflow chỉ chạy sau khi push và
  mở PR đầu tiên.
- **Chưa có SQL Server thật trong CI.** Bộ test dùng SQLite, nên phần `Encrypt=true` và `AD-19`
  (mỗi hãng một login SQL riêng) chưa có gì canh.
