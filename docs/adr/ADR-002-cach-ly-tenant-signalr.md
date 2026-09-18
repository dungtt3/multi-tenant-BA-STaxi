# ADR-002 — Cách ly tenant cho SignalR

- **Trạng thái:** Đề xuất (chưa chốt)
- **Ngày:** 2026-09-17
- **Liên quan:** D1 (.NET 8), D2 (một deployment, nhiều khách), phạm vi đợt đầu có màn hình **Online (SignalR)**

---

## Bối cảnh

Realtime nằm trong phạm vi đợt đầu. Dưới D2, SignalR là **khe rò rỉ tenant nguy hiểm nhất** trong toàn hệ: một bản tin phát nhầm không để lại log, không có mã lỗi, không ai báo — khách chỉ thấy xe của hãng khác nhảy trên bản đồ của mình.

### Đã đo được

| Dữ kiện | Hiện trạng |
|---|---|
| Thư viện | `Microsoft.AspNet.SignalR 2.2.0` — bản ASP.NET, **không phải Core** |
| Hub trong web | `BA.STaxi.Web/App_Start/RouteHub.cs` — hub duy nhất |
| Cách phát tin | `RouteHub.Send` → **`Clients.All.newPoint(...)`** — phát cho **tất cả**, không group, không tenant |
| Hub ngoài | `HubCenter` tại `server + "/Center/"`, tenant khai báo bằng **`hub.server.registerCompanyIds(hubCompanyId)`** — tenant **do client tự khai** |
| Trạng thái hub ngoài | Đang bị comment trong `_Navigation.cshtml` (`//ConnectSignalr(...)`) |
| Backplane | **Không cấu hình gì** — không `UseRedis`, không `UseSqlServer` |

Hai tiền lệ trong chính codebase này đều hỏng dưới D2:

- **`Clients.All`** an toàn hôm nay *chỉ vì* một deployment = một khách. Bỏ tiền đề đó đi thì nó là rò rỉ tức thì.
- **`registerCompanyIds(hubCompanyId)`** vi phạm đúng điều cấm của phiên thiết kế: *không bao giờ nhận tenant từ phía client*. Client sửa một biến JS là nhận tin của khách khác.

---

## Khảo sát bên ngoài: cả thế giới cũng chưa giải

Tìm toàn GitHub `"multitenant signalr"` → **đúng 1 repo, 0★**. Không có thư viện chuyên dụng nào. Kể cả **Finbuckle.MultiTenant** (1.621★, thư viện multi-tenancy .NET phổ biến nhất) **cũng không hỗ trợ SignalR hạng nhất**.

Nhưng issue **[Finbuckle #963](https://github.com/Finbuckle/Finbuckle.MultiTenant/issues/963)** (đóng 2025-03) đúng bài này, và câu trả lời đến từ chính tác giả thư viện:

1. **`IMultiTenantContextAccessor` luôn `null` trong Hub** — vì SignalR không có `HttpContext`. Chính tài liệu Microsoft khuyên tránh `IHttpContextAccessor` trong SignalR.
2. **Tenant context KHÔNG được giữ giữa `OnConnectedAsync` và các lần gọi method sau đó** — mỗi lần gọi hub method là **một DI scope mới**.

> ⚠️ Điều 2 bác bỏ cách làm trực giác nhất: *"phân giải tenant một lần lúc handshake rồi giữ"* — **không làm được**. Bản đầu của ADR này đã viết sai chỗ đó.

Cơ chế đúng, theo tác giả Finbuckle:

- Lúc kết nối, lưu tenant vào **`Context.Items`** (sống theo suốt connection).
- Viết một `IMultiTenantStrategy` đọc `Context.Items`.
- **Mỗi lần gọi hub method đều phân giải lại**: `tenantResolver.ResolveAsync(Context.Items)`.

## Yếu tố quyết định

1. Tenant phải được suy ra **ở server, lúc bắt tay kết nối**, từ danh tính đã xác thực — rồi **phân giải lại ở mỗi lần gọi method** từ `Context.Items`.
2. Không được tồn tại đường phát tin nào mà lập trình viên quên tenant vẫn biên dịch được.
3. Một deployment sẽ có nhiều instance → bắt buộc có backplane, và backplane cũng phải an toàn theo tenant.
4. Web cũ vẫn chạy SignalR 2.2.0 song song (Limousine ở lại web cũ — D5) → phải tính chuyện hai stack realtime cùng tồn tại.

---

## Các phương án

### A. Group theo tenant, suy ra ở server, cấm `Clients.All` ⭐ *(đề xuất)*

- Tenant phân giải **lúc handshake** từ token đã xác thực, không bao giờ từ tham số client gửi
- Tên group theo khuôn `t{TenantId}:{chủ-đề}`, sinh ra bởi một hàm duy nhất
- **Test kiến trúc làm đỏ build** khi gặp `Clients.All` / `Clients.Others` / `Clients.AllExcept`
- Backplane Redis, key có tiền tố theo môi trường **và** theo tenant
- ✅ Rẻ, hợp với SignalR Core, không đổi hạ tầng
- ⚠️ Vẫn dựa vào kỷ luật — nên chốt chặn phải là test, không phải hướng dẫn

### B. Hub riêng theo tenant (route hub động)

- ✅ Cách ly rõ ràng nhất về mặt khái niệm
- ❌ Số hub tăng theo số khách; quản lý vòng đời và giám sát phức tạp
- ❌ Backplane vẫn phải tự lo, không tự động an toàn hơn A

### C. Giữ realtime ở mô hình cũ — deployment riêng cho SignalR

- ✅ Không có rủi ro rò rỉ: mỗi khách một tiến trình
- ❌ Đi ngược D2 ở đúng phần khó nhất, và vẫn phải vận hành 18 bản
- ✅ Có thể dùng làm **bước đệm cho tenant thí điểm** (D7) trong lúc A chưa sẵn sàng

### D. Bỏ SignalR, dùng polling hoặc SSE

- ✅ Cách ly bằng chính mô hình request/response — dùng lại toàn bộ khe tenant của API
- ❌ Màn hình Online cần độ trễ thấp; polling đủ dày thì tốn hơn cả SignalR
- ⚠️ Đáng cân nhắc cho những màn hình *gần* realtime (dashboard số liệu), không cho bản đồ xe

---

## Đề xuất

**A**, kèm bốn ràng buộc không thương lượng:

1. Tenant chỉ đến từ **token đã xác thực lúc handshake**, ghi vào `Context.Items`, và **phân giải lại mỗi lần gọi hub method** (Finbuckle #963). `registerCompanyIds` kiểu client-khai bị loại bỏ; nếu client muốn thu hẹp phạm vi thì đó là **lọc bên trong tập đã được uỷ quyền**, không phải khai báo tập.
2. **Không tồn tại API phát tin nào không mang tenant.** Bọc `IHubContext` sau một lớp chỉ nhận `TenantScope` — `Clients.All` không nằm trong bề mặt gọi được.
3. **Tên group hai tầng** — `{hãng}:{companyId}:{chủ-đề}` (xem [ADR-004](ADR-004-mot-domain-dinh-danh-va-topo.md)), sinh bởi một hàm duy nhất. Đổi công ty ⇒ **rời group cũ, vào group mới**.
4. Backplane Redis có tiền tố `{môi trường}:{hãng}` và được kiểm bằng một bài test rò rỉ: hai tenant giả, phát tin ở tenant A, khẳng định tenant B **không nhận được gì**.

Cân nhắc **C cho tenant thí điểm** nếu cần ra mắt sớm hơn thời điểm A xong.

---

## Hệ quả

- **SignalR 2.2.0 → ASP.NET Core SignalR là viết lại cả client JS.** API `$.connection` không còn. Ảnh hưởng: `_Navigation.cshtml`, `_Navigation_Card.cshtml`, `Dashboard/Online.cshtml`.
- Web cũ giữ SignalR 2.2.0 (do Limousine ở lại — D5) → **hai stack realtime chạy song song**. Phải chốt: có dùng chung backplane không, và nếu có thì ai là chủ của không gian key.
- Hub ngoài `HubCenter` (`/Center/`) thuộc hệ thống khác → cần một ADR riêng, hoặc ít nhất một cuộc nói chuyện với chủ sở hữu nó trước khi web mới kết nối.
- Bài test rò rỉ hai-tenant nên là **một trong những test đầu tiên của dự án**, không phải test cuối.

## Việc còn mở

- [ ] `HubCenter` do hệ thống nào sở hữu? Nó có sẵn khái niệm tenant ở phía server chưa?
- [ ] Vì sao `ConnectSignalr` đang bị comment — tắt tạm hay đã bỏ hẳn?
- [ ] Màn hình Online cần độ trễ bao nhiêu, bao nhiêu xe mỗi tenant lúc cao điểm? (quyết định giữa A và D cho từng màn hình)
