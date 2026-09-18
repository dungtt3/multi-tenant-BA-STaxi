# Core — cái móng, và khi nào thì gọi là xong

Nguyên tắc: **một module nghiệp vụ mới không được phép quyết định lại bất cứ điều gì trong danh sách này.**
Nếu người viết module thứ ba phải tự nghĩ ra cách cấp token, cách khai quyền, hay cách ghi vết — thì core
chưa xong, dù code có chạy.

Đây là thước đo: *dựng module thứ hai có phải copy-paste từ module thứ nhất không?* Nếu có, chỗ bị
copy-paste chính là chỗ core còn thiếu.

---

## Đã xong

| | Ràng buộc | Bằng chứng |
|---|---|---|
| `Staxi.Platform` — `TenantScope`, `ITenantConnectionFactory`, `CacheKey`, ProblemDetails, `IClock`, SignalR wrapper | `AD-2` `AD-4` `AD-6` `AD-7` `AD-8` `AD-16` | 15 luật kiến trúc |
| `Staxi.TenantLeakTests` — 50 bài, hai hãng giả | `AD-24` | Đã chứng minh có răng: gỡ lớp bảo vệ thì đỏ |
| `Staxi.ArchitectureTests` — 15 luật quét IL | `AD-15` | R1–R15, quét toàn bộ assembly sản phẩm |
| `db/tenancy-classes.json` — 279 bảng | `AD-5` | R12–R14 ép; duyệt theo nhu cầu |
| CI — lint, test, burn-in, frontend, hai cổng ratchet | `AD-24` | Chạy mỗi PR |
| `apps/admin-web` — Vite + React 19 + token CSS + `.ibox` | `AD-1` | Build ra file tĩnh |
| `Staxi.Admin.*` — lát cắt mẫu, đọc bảng thật | — | Khuôn cho mọi module sau |

---

## Còn thiếu, theo thứ tự phải làm

### 1. Đường mở kết nối cho xác thực

Xác thực diễn ra **trước** khi có `TenantScope`, nên `ITenantConnectionFactory` — vốn đòi phải có phạm vi —
không phục vụ được luồng đăng nhập. Nếu không có đường riêng, người viết `Staxi.Auth` sẽ phải phá luật
`AD-2` hoặc bịa một `TenantScope` giả.

Cần: một cổng đặt tên tường minh, bắt khai lý do, và **chỉ `Staxi.Auth` được gọi** — ép bằng luật kiến trúc,
giống cách `AcrossTenants(lyDo)` làm với việc vượt phạm vi.

### 2. `Staxi.Auth` — đăng nhập và cấp token

`AD-3` `AD-4` `AD-22`. Không có nó thì không module nào chạy được end-to-end.

Đã biết đủ schema để dựng: `[Admin.Users]` với `UserName`, `Password` (MD5 không salt), `FK_CompanyID`
(không null), `IsLock`, `IsDeleted`, và `CurrentCompanyId` — chỗ web cũ lưu "đang xem công ty nào".

Bốn thứ bắt buộc:
- Mã hãng sai và mật khẩu sai trả **cùng một thông báo, cùng thời gian phản hồi**
- Rate limit tính cả mã hãng không tồn tại
- Token mang `tenantCode` + `companyId` + `permissionVersion` + `securityStamp`
- `select-company` **kiểm lại quyền từ DB** rồi cấp token mới — không sửa token cũ

### 3. Sổ đăng ký quyền

`AD-17`. Nếu thiếu, module thứ hai sẽ tự nghĩ ra cách khai quyền, và hai cách khai quyền là hai lỗ hổng.

Cần: hằng cho mỗi quyền theo khuôn `{TàiNguyên}.{HànhĐộng}`, `[HasPermission(...)]` trên endpoint,
`PermissionSet` dựng **một lần mỗi request** tra O(1), và `permissionVersion` bump bằng **trigger** chứ
không bằng code ứng dụng — vì chỉ trigger mới thấy được cả hai hệ.

### 4. Test tích hợp trên SQL Server thật

Khoảng trống vừa lộ ra ngày 2026-09-18: câu SQL của `LoaiXeRepository` chỉ từng chạy trên SQLite. Kiểm tay
trên SQL Server thì đúng, nhưng **không có cơ chế nào buộc** điều đó xảy ra trước khi merge.

Bộ test rò rỉ dùng SQLite là đúng — nó phải chạy được ở mọi nơi. Nhưng cần thêm một tầng chạy trên SQL
Server thật, có thể bỏ qua khi không có kết nối, và **bắt buộc chạy trong CI trước khi lên production**.

### 5. Gateway (YARP)

`AD-3` đòi **gateway xoá mọi header `X-Tenant*` / `X-Company*` đến từ ngoài**. Hiện chỉ có middleware trong
ứng dụng làm việc đó — tức lớp phòng thủ ngoài cùng chưa tồn tại.

### 6. BFF

`AD-12`. Một màn hình nên là **một** lời gọi. Không có BFF thì mỗi màn hình tự gom qua nhiều chặng mạng, và
chỗ gom đó sẽ mọc nghiệp vụ.

### 7. Sổ vết thật

`AD-18`. `LoggingAcrossTenantAuditSink` hiện chỉ ghi log — đó là chỗ giữ chỗ có ý thức. Cần kho chỉ-ghi-thêm
tách khỏi database nghiệp vụ, và **trigger database** cho lệnh ghi (vì web cũ ghi ngoài tầm với).

### 8. Sổ đăng ký hãng thật

`AD-14`. Hiện là `InMemoryTenantRegistryStore`. Cần bảng trong DB quản trị riêng, connection string mã hoá
phong bì, khoá ngoài mã nguồn, và **công tắc vô hiệu một hãng cắt cả phiên đang chạy**.

### 9. Redis

`AD-6` cache và `AD-7` backplane. Hiện cache là trong tiến trình và SignalR chạy một tiến trình — nghĩa là
rò rỉ xuyên tiến trình chưa có gì canh.

### 10. Quan trắc

`AD-13`. OpenTelemetry, `traceparent` W3C, danh sách **trắng** trường được ghi.

### 11. `db/screen-owners.json`

`AD-20`. Nhỏ, nhưng phải có **trước** khi màn hình đầu tiên được tuyên bố là đã chuyển.

### 12. Đường chứng chỉ cho SQL Server

Câu hỏi mở `Q4`, giờ đã có ca thật: `Microsoft.Data.SqlClient` mặc định `Encrypt=true` nên mọi kết nối tới
IP nội bộ fail. Đang tạm dùng `TrustServerCertificate` khi dò — **nợ phải trả trước khi có môi trường chạy thật**.

---

## Không thuộc core

Nghiệp vụ: danh mục, CRUD cấu hình, dashboard, báo cáo. Chúng là **module** lắp lên móng, và mỗi module
phải mọc thêm bài trong `Staxi.TenantLeakTests` theo `AD-24`.

`Staxi.Reporting` cũng vậy — nó là module, dù `AD-9` cho phép giữ proc chỉ-đọc trong SQL.
