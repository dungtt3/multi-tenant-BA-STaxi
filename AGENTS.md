# multi-tenant-BA-STaxi (WEB2)

Nền tảng admin multi-tenant mới của BA STaxi / G7 Taxi. React + .NET 10 + YARP gateway.
Chạy **song song** với admin cũ (`../ba_staxi_webadmin`, ASP.NET MVC 5), **dùng chung 18 database khách hàng**.

**`docs/ARCHITECTURE-SPINE.md` là luật.** 24 quyết định kiến trúc (`AD-1`…`AD-24`) ràng buộc mọi dòng code trong repo này. Đọc nó trước khi làm việc không tầm thường. Tài liệu này chỉ là bản rút gọn để làm việc hằng ngày — khi hai bên lệch nhau, **spine đúng**.

---

## Từ vựng — sai chỗ này là rò rỉ dữ liệu

| Từ | Nghĩa chính xác | Kiểu |
|---|---|---|
| **hãng** (`tenantCode`) | Một khách hàng = **một database riêng**. Gõ ở màn đăng nhập. | `string` |
| **công ty** (`companyId`) | Một đơn vị **bên trong** một hãng — cột `CompanyId` sẵn có. | `int` |
| **`TenantScope`** | Cặp bất biến `(tenantCode, companyId)` của request. **Đây** là "tenant" trong mọi AD. | kiểu riêng |

Ba đội đọc "lọc theo tenant" thành ba việc khác nhau là cách tái tạo đúng lỗ hổng `ChangeCompany` của web cũ.

---

## Luật không thương lượng

Mỗi dòng dưới đây có một cơ chế ép trong CI — bảng đầy đủ ở `AD-15`. Vi phạm là **build đỏ**, không phải góp ý.

- **Phạm vi chỉ đến từ token đã ký.** Cấm suy ra `tenantCode`/`companyId` từ bất kỳ dữ liệu nào client gửi — query, body, header, form, route, tham số hub — **bất kể đặt tên là gì**. (`AD-4`)
- **`ITenantConnectionFactory` là đường duy nhất tới database.** Cấm `new SqlConnection(...)`. Factory **không nhận tham số tenant** — nó đọc `TenantScope` của request. Không có `TenantScope` ⇒ ném lỗi. (`AD-2`)
- **Mỗi bảng có đúng một hạng tenant** — `Global` / `PerTenant` / `PerCompany` / `CrossLink`, khai báo trong `db/tenancy-classes.json`. Chưa phân hạng ⇒ build đỏ. (`AD-5`)
- **`CacheKey` không khởi tạo được nếu thiếu `TenantScope`.** Key dùng chung chỉ cho mục có trong sổ `SharedReferenceData`. Mỗi hãng có DB riêng nên **danh mục là của riêng hãng đó** — tỉnh thành, loại xe ở hãng A có id khác hãng B. (`AD-6`)
- **SignalR:** phạm vi lấy từ token lúc handshake, ghi vào `Context.Items`, **phân giải lại từ đó ở mỗi lần gọi hub method**. Cấm `Clients.All` / `Others` / `AllExcept`. Cấm lấy phạm vi từ tham số hub. (`AD-7`)
- **API trả mã trạng thái HTTP thật.** Lỗi là RFC 9457 ProblemDetails + `corrId`. **Cấm HTTP 200 cho một lỗi** — đây là sai lầm lớn nhất của web cũ. (`AD-8`)
- **Thời gian:** cột kế thừa đọc–ghi **giờ địa phương GMT+7** đúng như web cũ; cột mới dùng UTC và **bắt buộc hậu tố `Utc`**; trên dây luôn ISO-8601 có offset; trong code cấm `DateTime.Now`/`UtcNow`, dùng `IClock`. (`AD-16`)
- **Proc có ghi phải viết lại trong .NET**, dù có `GROUP BY` hay không. Chỉ proc **chỉ-đọc + gom dữ liệu** mới được giữ trong SQL và bọc Dapper. (`AD-9`)
- **DTO, enum, hình dạng dây định nghĩa một lần** trong `Staxi.Contracts`. Cấm mỗi dịch vụ tự định nghĩa lại. (`AD-8`)
- Cấm trạng thái static thay đổi được · cấm `HttpContext` ngoài đúng một adapter · cấm singleton giữ dịch vụ scoped.

---

## Dùng chung database với web cũ — ràng buộc sống còn

`BA.STaxi.Web` vẫn chạy và **vẫn ghi** vào cùng 18 database. Cho tới khi một hãng bỏ hẳn web cũ:

| | |
|---|---|
| ✅ Được | thêm cột `NULL`-able · bảng mới · index · proc mới |
| ⛔ Cấm | đổi tên cột · đổi kiểu · xoá cột · **đổi nghĩa giá trị** · sửa proc web cũ đang gọi theo cách đổi hình dạng kết quả |

Hai hệ quả dễ quên:

- **Bất biến nghiệp vụ chỉ tồn tại trong code .NET là bất biến không có thật.** Tầng Domain không ép được gì khi một ứng dụng khác ghi song song — bất biến sống còn phải là constraint trong database. (`AD-11`)
- **Ghi đè im lặng là rủi ro đã chấp nhận** (last-write-wins). Đổi lại, vết phải bắt bằng **trigger database**, đo tại kết nối vật lý (`DB_NAME()`, `ORIGINAL_LOGIN()`), lưu giá trị trước–sau. Không bắt bằng code ứng dụng, vì web cũ ghi ngoài tầm với. (`AD-18`)

---

## Nơi mọi thứ nằm

```
docs/ARCHITECTURE-SPINE.md    # LUẬT — 24 AD, quy ước, stack, câu hỏi mở
docs/adr/                     # 6 ADR: lệch phiên bản DB · SignalR · schema 18 DB · một domain + topo
                              #        · thư viện component FE · bảng dữ liệu và biểu đồ
docs/design-guide.md          # token màu/chữ/khoảng cách, responsive, luật "lấy thiết kế không lấy code"
docs/ghi-chu-thiet-ke-platform.md  # vì sao code nền tảng trông như vậy (code không mang comment)
docs/ci.md                    # pipeline chất lượng: đỏ ở đâu thì làm gì
docs/architecture-deck.html   # bản trình bày cho team (27 slide, mở bằng trình duyệt)
docs/reviews/                 # 4 review độc lập của spine — còn nhiều phát hiện medium/low chưa đưa vào spine
docs/evidence/                # danh sách 2.003 proc, và 279 proc mã nguồn cũ thực sự gọi tới
docs/.architecture-memlog.md  # trí nhớ phiên kiến trúc — nguồn để cập nhật spine, đừng sửa spine mà bỏ qua nó
```

Repo cũ `../ba_staxi_webadmin` giữ memlog brainstorm và `brainstorm-intent.md` làm lịch sử.

---

## Công cụ trên máy này

- .NET SDK **10.0.102** · Node **v24.13.0** · npm **11.6.2** — đủ, không cần cài thêm.
- Phiên bản thư viện đã kiểm chứng trên NuGet/npm ngày 2026-09-17: xem bảng Stack trong spine. **Đừng tự nâng** — mấy dòng ghim có lý do viết ngay dưới bảng.

Hai cái bẫy đã biết, đọc trước khi mất buổi chiều:

- **`Microsoft.Data.SqlClient` mặc định `Encrypt=true` từ bản 4.0.** 18 SQL Server chạy trên IP nội bộ sẽ **fail toàn bộ kết nối lúc khởi động**. Xử lý bằng chứng chỉ, không bằng `TrustServerCertificate=true`.
- **`StackExchange.Redis` ghim 2.13.17, không phải 3.x.** `SignalR.StackExchangeRedis 10.0.12` khai báo phụ thuộc 2.7.27; NuGet sẽ nâng lên 3.x và **build vẫn xanh**, lỗi chỉ hiện lúc chạy.
- **TypeScript ghim 5.9.3.** `typescript-eslint` chưa hỗ trợ TS 7.

---

## Quy ước

- **`.cs` là UTF-8 *không* BOM, CRLF.** Repo này **không** kế thừa luật BOM của `ba_staxi_webadmin` — luật đó sinh ra từ ASP.NET cũ, không áp dụng ở đây.
- Namespace `Staxi.{Service}.{Domain|Application|Infrastructure|Api}`; hợp đồng dùng chung ở `Staxi.Contracts`.
- Interface `I{X}Service` / `I{X}Repository`; trường private `_camelCase`; endpoint `kebab-case`; quyền `{TàiNguyên}.{HànhĐộng}`.
- Tiếng Việt cho chuỗi giao diện và bình luận, theo lệ của sản phẩm.
- Truy cập dữ liệu bằng **Dapper**, qua repository có phạm vi tenant.

---

## Việc chưa xong — biết trước khi bị bất ngờ

Chín câu hỏi mở nằm cuối spine. Ba câu chặn nhiều thứ nhất:

1. **DBA có cấp được 18 login SQL riêng không?** (`AD-19`, chưa xác nhận) — một login dùng chung biến *mọi* lỗi code thành rò rỉ dữ liệu xuyên hãng. Đây là chốt chắn rẻ nhất và mạnh nhất của cả spine.
2. **18 DB đang trôi schema bao nhiêu?** Chưa ai đo. Không có con số đó thì việc hoãn ADR-001 là canh bạc mù.
3. **Ai sở hữu `[Admin.DbMigrations]` và `[Version.ApiSupported]`?** Hai bảng đã tồn tại trong DB, không repo nào trong tầm nhìn tham chiếu tới.

Và một việc **độc lập hoàn toàn với repo này**: `HomeController.ChangeCompany` ở web cũ không thấy kiểm tra quyền — đọc code thì bất kỳ user đăng nhập nào cũng đổi sang công ty bất kỳ. Cần xác minh trên hệ chạy thật và vá ở web cũ.

---

## Thứ tự làm việc mà spine đề ra

1. ✅ `Staxi.Platform` — `TenantScope`, `ITenantConnectionFactory`, `CacheKey`, ProblemDetails, tracing.
2. ✅ **`Staxi.TenantLeakTests` — bộ test rò rỉ hai tenant, viết TRƯỚC tính năng đầu tiên** (`AD-24`). 45 bài, hai hãng giả.
3. ✅ `Staxi.ArchitectureTests` — bảng ép của `AD-15`. 11 luật quét IL.
4. ⬜ Rồi mới tới tính năng: danh mục, CRUD config, dashboard.

Ba bước đầu xong ngày 18-09-2026 (56/56 xanh, CI chạy bộ test rò rỉ mỗi PR). `apps/admin-web` chưa có dòng nào — đọc `docs/design-guide.md` và ADR-005/006 trước khi dựng.

Spine này **chưa được chứng minh, mới được rà**. Bốn người duyệt tìm ra ~100 lỗi trong bản đầu. Thứ sẽ phát hiện phần còn lại là `AD-24` và hãng thí điểm đầu tiên — không phải thêm một vòng review.

<!-- agent-ninja-START -->
## Agent Skills

> **IMPORTANT**: Prefer skill-led reasoning over pre-training-led reasoning.
> See [Agent Skills](.github/skills/README.md) before working on tasks covered by these skills.

<!-- agent-ninja-END -->
