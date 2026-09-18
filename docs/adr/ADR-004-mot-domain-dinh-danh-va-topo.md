# ADR-004 — Một domain mới, chọn hãng tại màn đăng nhập, và topo triển khai

- **Trạng thái:** Đề xuất (chưa chốt)
- **Ngày:** 2026-09-17 *(đã sửa sau khi chốt cách chọn hãng)*
- **Liên quan:** D1 (.NET 8), D2 (một deployment, DB tách riêng), D3 (API + gateway), D7 (thí điểm trước)
- **Kèm theo:** [ADR-001](ADR-001-lech-phien-ban-db.md) · [ADR-002](ADR-002-cach-ly-tenant-signalr.md) · [ADR-003](ADR-003-chien-luoc-schema-18-db.md)

---

## Quyết định

1. **Một domain mới hoàn toàn**, chứa tất cả các hãng. Không liên quan tới domain riêng của từng hãng hiện tại.
2. **Chọn hãng ngay tại màn đăng nhập** — trước khi xác thực, không phải sau.
3. **WEB2 là một trang web riêng biệt.** Dùng chung với hệ cũ **đúng một thứ: database + connection.** Không chung phiên, không chung cookie, không cầu SSO.
4. User thuộc nhiều công ty **trong hãng đó** → màn chọn công ty sau khi đăng nhập.

Kèm theo: FE React tĩnh · BE .NET Core · Dapper cho tầng dữ liệu · stored procedure viết lại **từng cái** khi lần lượt chuyển báo cáo sang WEB2.

---

## Điều quan trọng nhất: quyết định này giải tán ba rủi ro

Bản đầu của ADR này giả định tenant phân giải **sau** khi đăng nhập. Chọn hãng **tại màn đăng nhập** làm ba vấn đề lớn nhất biến mất:

| Vấn đề ở bản đầu | Trạng thái |
|---|---|
| **Con gà–quả trứng**: chưa biết hãng thì chưa biết mở DB nào để tra user | ✅ **Tan biến.** Biết tenant *trước* khi xác thực → mở đúng DB của hãng đó → tra user ở bảng user sẵn có |
| **Phải xây DB danh tính trung tâm** (thành phần mới, chưa từng có) | ✅ **Không cần nữa** |
| **Va chạm username giữa 18 DB** (`admin`, `quantri`… trùng nhau) | ✅ **Không còn là vấn đề.** Username chỉ cần duy nhất *trong một hãng* — điều đã đúng từ trước tới nay |
| **Đổi hãng = đổi cả database nóng** | ✅ **Không cần.** User thuộc hai hãng thì đăng nhập hai lần với lựa chọn khác nhau. `TenantConnectionFactory` không phải xử lý chuyển đổi nóng |

Cảnh báo "rủi ro số 1 — va chạm username" ở bản trước **không còn hiệu lực**.

---

## Thứ duy nhất còn phải xây: sổ đăng ký hãng

Không phải DB danh tính — chỉ là **danh sách hãng + connection string**. Nhưng nó giữ 18 connection string **có mật khẩu**, nên:

- ⛔ **Không phải bảng thường**, không phải file config đọc được bằng tài khoản ứng dụng.
- ✅ Secret store (Azure Key Vault / HashiCorp Vault / DPAPI+Redis), hoặc config mã hoá với khoá ngoài mã nguồn.
- ⚠️ **Nó là điểm chết đơn lẻ mới**: nó chết thì **không hãng nào đăng nhập được**. Cần cache cục bộ + health check riêng.

Khớp với `ITenantStore` trong `dungtt3/Permission` — chỉ dùng phần lưu tenant, bỏ phần identity.

---

## ⚠️ Rủi ro mới 1 — trang login lộ danh sách khách hàng

Một dropdown liệt kê tất cả hãng ở trang đăng nhập nghĩa là **bất kỳ ai mở trang cũng thấy toàn bộ danh sách khách hàng** của anh. Với một sản phẩm đang bán cho nhiều hãng taxi, đó là thông tin kinh doanh.

### ✅ Đã chốt

**Gõ mã hãng + nhớ lựa chọn trong `localStorage`. Không có dropdown, không có gợi ý.**

| | |
|---|---|
| Được | Danh sách khách hàng **không lộ ra ở bất kỳ đâu**. Không có endpoint nào trả danh sách hãng cho người chưa đăng nhập |
| Mất | Người dùng phải nhớ mã hãng lần đầu |
| Bù | `localStorage` nhớ mã đã dùng ⇒ từ lần thứ hai trở đi ô mã đã điền sẵn. Xoá cache thì phải gõ lại |

Ràng buộc kèm theo:

- ⛔ **Không được có API `GET /tenants`** hay bất kỳ endpoint nào liệt kê hãng khi chưa xác thực.
- ⛔ **Mã hãng sai và mật khẩu sai phải trả về cùng một thông báo.** Nếu mã hãng sai báo khác đi thì kẻ tấn công dò được danh sách hãng bằng cách thử mã — đúng thứ vừa cố tránh.
- Mã hãng nên **không đoán được theo quy luật** (tránh `1`, `2`, `3` hoặc tên thương hiệu trần).
- Rate limit phải tính cả trường hợp **mã hãng không tồn tại**, nếu không việc dò mã là miễn phí.
- Phương án dự phòng cho người dùng quên mã: gửi qua kênh đã xác thực (email quản trị hãng), **không phải một trang tra cứu công khai**.

---

## ⚠️ Rủi ro mới 2 — hai ứng dụng cùng **ghi** vào một database

Đây là hệ quả nặng nhất của "dùng chung DB + connection", và nó **chặt hơn giả định của ADR-001**.

ADR-001 lo lệch phiên bản giữa *API mới và DB*. Giờ còn một chiều nữa: **web cũ và WEB2 chạy đồng thời trên cùng một DB.**

**Nghĩa là: mọi thay đổi schema phải tương thích ngược với web cũ**, cho tới khi hãng đó ngừng dùng web cũ hẳn.

Luật cụ thể cho giai đoạn song song:

- ✅ Thêm cột `NULL`-able, thêm bảng mới, thêm index — an toàn
- ⛔ Đổi tên cột, đổi kiểu, xoá cột, đổi nghĩa giá trị — **cấm** khi hãng còn dùng cả hai
- ⚠️ Một bảng, **một người ghi**. Nếu buộc phải hai, viết hợp đồng tường minh + test tích hợp hai chiều
- Phải bổ sung luật này vào [ADR-003](ADR-003-chien-luoc-schema-18-db.md)

---

## Luồng đăng nhập

```
Màn đăng nhập:  [ chọn/gõ hãng ]  [ username ]  [ mật khẩu ]
       │
       ├─ hãng  → tra sổ đăng ký → connection string của hãng đó
       │
POST /auth/login  → xác thực trên DB của hãng (bảng user sẵn có)
       │
       ├─ user chỉ có 1 công ty → cấp token đầy đủ luôn
       └─ user có nhiều công ty → màn chọn công ty
                                        │
                          POST /auth/select-company
                            → KIỂM TRA LẠI quyền → CẤP TOKEN MỚI
                               (tenant + companyId + permissionVersion)
```

### Luật không thương lượng

1. **Không bao giờ nhận `companyId` từ request.** Nó nằm trong token đã ký.
2. **Đổi công ty = cấp lại token**, không phải gửi header khác.
3. `select-company` **kiểm tra lại** quyền — không tin danh sách đã trả trước đó.
4. `PermissionSet` **dựng lại theo công ty vừa chọn** — đúng lỗi web cũ đang mắc (xem dưới).
5. Mọi lần đổi phạm vi đều **ghi vết**: corrId lưu cả tenant thực thi lẫn tenant hiệu lực.

### Bảo mật của trang login dùng chung

- Thông báo lỗi **đồng nhất**: không được phân biệt "sai mật khẩu" với "user không tồn tại" hay "hãng không có user này".
- Rate limit theo **IP**, theo **username**, và theo **(hãng, username)**.
- Danh sách công ty chỉ trả **sau khi** xác thực thành công.

---

## Web cũ đã làm màn chọn công ty — và làm sai hai chỗ

`BA.STaxi.Web/Controllers/HomeController.cs` đã có `ChangeCompany(string id)`.

**⚠️ Không thấy kiểm tra quyền.** Method không có `[Authenticate]`, `HomeController` không có filter cấp lớp, và nó tra `CompanyID` **toàn cục** rồi gán thẳng:

```csharp
var company = _adminCompanyService.FindBy(x => x.CompanyID == companyid).FirstOrDefault();
if (company == null) return new JsonResult();
var auth = AuthenticationHelper.LoggedInUserEntity;
auth.CurrentCompanyId = companyid;     // ← không đối chiếu với quyền của user
```

Đọc code thì **bất kỳ user đã đăng nhập nào cũng đổi được sang bất kỳ công ty nào trong hãng đó**. *Cần xác minh trên hệ chạy thật — nhưng phải xác minh sớm, vì nếu đúng thì đây là lỗ hổng đang mở trên production, cần vá ở web cũ ngay chứ không chờ WEB2.*

**⚠️ Quyền tính lại nhưng không scope theo công ty vừa chọn.** `GetRoleLastest(...)` lấy role mà **không lọc theo `companyid`**. Đoạn code cũ bị comment ngay phía trên có đúng dòng `.Where(p => p.FK_CompanyID == companyid)` — **bộ lọc đó đã biến mất ở bản đang chạy.**

→ **WEB2 không port logic này. Viết lại.**

---

## Topo triển khai

```
                    Internet
                       │
                  HAProxy (biên: TLS, health, phân tải)
                       │
                  YARP Gateway (tenant + version ADR-001 + affinity)
                       │
        ┌──────────────┼──────────────┐
   /api/web/*      /signalr/*         /
        │              │              │
   BE .NET Core   notification   React tĩnh
   (N instance)   (affinity)     (CDN / nginx)
```

**Tiền lệ đã có của chính anh** — `dungtt3/BAE`, `BaExpress.Gateway/routing.conf.json`: định tuyến theo tiền tố đường dẫn tới các cluster BFF riêng, route `/signalr/` với `Metadata.WebSocket = true`, `notif-cluster` đã bật `SessionAffinity: Cookie / .Yarp.Affinity`, cộng Data Protection trên Redis trong `Program.cs`.

### Ba điều phải trả giá

**a) Không còn `Session`.** BE phải **stateless bằng JWT** — `dungtt3/Permission` đã có `SecurityStamp` + `PermissionVersion` để thu hồi tức thì. Stateless ⇒ HAProxy round-robin thuần, không cần sticky cho phần API.

**b) SignalR là ngoại lệ duy nhất** ⇒ để thành **cluster riêng**, đừng trộn vào BE chính. Phía HAProxy cần **`timeout tunnel`** đủ dài, nếu không WebSocket bị cắt theo `timeout client/server`.

**c) Connection pool nhân lên.** Trước: 1 site = 1 DB. Giờ **1 app × 18 DB × N instance**, mỗi connection string là một pool riêng của ADO.NET. `Max Pool Size` mặc định 100 ⇒ `18 × 6 × 100 =` **10.800 kết nối tiềm năng** về phía SQL. Phải đặt có chủ đích và giám sát.

### React: một bản build, không phải một site mỗi hãng

Build tenant-agnostic. Thương hiệu/màu/logo từng hãng là **dữ liệu từ API config**, không phải build-time — build riêng mỗi hãng là tái tạo đúng nỗi đau 18 bản deploy, chỉ đổi chỗ sang frontend. Và nó là file tĩnh: đẩy lên `Cdn-File-Service` sẵn có hoặc nginx, đừng làm IIS site.

---

## Chạy song song trở nên tầm thường

Vì WEB2 là trang web riêng trên domain riêng, chỉ chung database:

- ✅ Không cầu SSO, không chia sẻ cookie/session
- ✅ Web cũ **không bị đụng tới** — 18 site IIS hiện tại chạy nguyên trạng
- ✅ Lùi về = bảo người dùng quay lại URL cũ. Không có bước kỹ thuật nào
- ✅ Thí điểm theo hãng (D7) = chỉ cần cho hãng đó biết domain mới
- ⚠️ Đổi lại: **kỷ luật schema tương thích ngược** trở thành ràng buộc sống còn (rủi ro mới 2)

---

## Hệ quả dây chuyền lên các ADR khác

- **Cache key hai tầng**: `{hãng}:{companyId}:…`
- **SignalR group** `{hãng}:{companyId}:{chủ-đề}`, lấy từ token lúc handshake, phân giải lại mỗi lần gọi method (ADR-002)
- **Đổi công ty** ⇒ rời group SignalR cũ, vào group mới, xả cache phía client
- **ADR-003 phải bổ sung** luật schema tương thích ngược trong giai đoạn hai app chung DB

## Việc còn mở

- [ ] Sổ đăng ký hãng đặt ở đâu, bảo vệ bằng gì, và ai được đọc?
- [ ] Mã hãng đặt theo quy tắc nào (không đoán được, không phải số thứ tự)? Có tái dùng `XNCode` sẵn có không?
- [ ] **Xác minh lỗ hổng `ChangeCompany` trên hệ chạy thật** — nếu đúng thì vá ở web cũ ngay
- [ ] 1 HAProxy hiện là điểm chết đơn lẻ; gom 18 hãng về sau nó thì bán kính ảnh hưởng tăng gấp 18 — đã có cặp active-passive chưa?
- [ ] Domain mới: tên là gì, cert ở đâu, ai cấp?
