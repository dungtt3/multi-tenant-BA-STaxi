---
name: 'Review đối kháng — ARCHITECTURE-SPINE (WEB2)'
type: review
lens: adversarial
target: ARCHITECTURE-SPINE.md
reviewer: adversarial-gate
date: '2026-09-17'
verdict: 'KHÔNG ĐẠT — spine chưa đủ chặt để hai đội làm song song'
---

# Review đối kháng — ARCHITECTURE-SPINE WEB2

## Cách đọc bản review này

Mỗi mục là **một cặp đơn vị cụ thể một cấp dưới spine** (hai đội, hai màn hình, hai dịch vụ).
Cả hai đơn vị **tuân thủ từng chữ mọi AD** đang có, nhưng vẫn ráp vào nhau **không khớp**.
Mỗi cặp như vậy là một lỗ. Với mỗi lỗ: cặp đơn vị → cách cả hai tuân thủ → hậu quả → **AD mới hoặc AD siết lại** → mức nghiêm trọng.

Tôi không tính các lỗi chính tả, cách hành văn hay chỗ diễn đạt chưa hay. Chỉ tính chỗ **spine không quyết, nên hai đội tự quyết khác nhau**.

**Kết luận sớm:** spine này tốt ở lớp an ninh chu vi (AD-3, AD-4, AD-7, AD-15) nhưng **hổng ở lớp từ vựng chung**. Nó nói rất kỹ *ai được gọi ai*, và gần như không nói *hai bên phải gọi cái gì là cùng một thứ*. Với 18 DB, hai ứng dụng cùng ghi và 565 bảng kế thừa, đó là chỗ dự án sẽ vỡ.

---

## Bảng tổng hợp lỗ

| # | Lỗ | AD liên quan | Mức |
|---|---|---|---|
| L-01 | `ITenantOwned` không định nghĩa "tenant" là DB hay `CompanyId` | AD-5, AD-2, AD-4 | **critical** |
| L-02 | AD-4 cấm theo *tên biến*, `X-Tenant-Code` lọt qua khe cửa | AD-4, AD-3 | **critical** |
| L-03 | Convention "UTC ISO-8601" đâm thẳng vào 1.288 chỗ `DateTime.Now` của web cũ | Conventions, AD-10 | **critical** |
| L-04 | Ghi vết của AD-11 chỉ có một nửa: web cũ có sổ vết riêng, khác hình dạng, và tắt được | AD-11 | **critical** |
| L-05 | Dữ liệu "toàn cục" không tồn tại về mặt vật lý khi có 18 DB — AD-6 lại cho phép key dùng chung | AD-6, AD-5, AD-2 | **critical** |
| L-06 | AD-7 tự mâu thuẫn: "lấy từ token lúc handshake, ghi `Context.Items`" vs "phân giải lại mỗi lần gọi" | AD-7, AD-4 | **critical** |
| L-07 | `permissionVersion` không ai bump khi **web cũ** đổi quyền | AD-4, AD-11 | **critical** |
| L-08 | Proc vừa gom vừa ghi rơi đúng vào khe giữa hai vế của AD-9 → ghi không có vết, không có lọc tenant | AD-9, AD-11, AD-15 | **critical** |
| L-09 | Không có sổ chủ sở hữu màn hình: web cũ và WEB2 cùng mở một màn hình sửa mãi mãi | AD-11, paradigm | **high** |
| L-10 | Bảng "liên kết chéo" không định nghĩa vị từ phạm vi (chủ sở hữu vs bên tham gia) | AD-5 | **high** |
| L-11 | `{vùng}` và hình dạng giá trị trong cache không có sổ đăng ký → trùng key, khác DTO | AD-6 | **high** |
| L-12 | `permissionVersion` đóng tem "khi nội dung phụ thuộc quyền" — bên ghi và bên đọc phán đoán khác nhau | AD-6, AD-4 | **high** |
| L-13 | `{chủ-đề}` SignalR tự do → hai đội trùng tên group, hai payload | AD-7 | **high** |
| L-14 | Mã quyền trôi dạt: cùng một năng lực, hai chuỗi khác nhau, CI vẫn xanh | AD-15, AD-4 | **high** |
| L-15 | Không có kiểu/enum/DTO dùng chung → `status: 2` và `status: "DangCho"` cùng một chuyến | Conventions, cây nguồn | **high** |
| L-16 | Ranh giới "gom dữ liệu" vs "nghiệp vụ" của AD-12 phân định được hai cách | AD-12 | **high** |
| L-17 | `corrId` không nói ai sinh → một màn hình ba corrId, phá luôn AD-11 | AD-8, AD-13, AD-11 | **high** |
| L-18 | grate: Once đổi nội dung sau khi chạy, và AD-10 cấm đổi hình dạng proc mà không có cách phát hiện | AD-10 | **high** |
| L-19 | 18 DB lệch mức schema giữa chừng, không có luật "code phải chịu được cột chưa có" | AD-10, Deferred #1 | **high** |
| L-20 | AD-15 liệt kê 7 chốt chắn cho ~20 lệnh cấm — phần còn lại chỉ là lời khuyên | AD-15 (tất cả) | **high** |
| L-21 | Redis dùng chung cho cache và backplane nhưng hai tiền tố khác nhau, cache thiếu `{môi-trường}` | AD-6, AD-7 | **medium** |
| L-22 | Rate limit của AD-3 không định nghĩa chiều khoá → một hãng khoá cửa 17 hãng còn lại | AD-3, AD-2 | **medium** |
| L-23 | Cache sổ đăng ký hãng không có TTL/thu hồi → xoay mật khẩu DB, nửa cụm dùng chuỗi cũ | AD-14 | **medium** |
| L-24 | JSON casing không được chốt trong Conventions | Conventions | **medium** |
| L-25 | Tải file (Excel) không nằm gọn trong "một màn hình một lời gọi BFF" | AD-12 | **low** |

---

## L-01 — `ITenantOwned` không nói "tenant" là **database** hay là **`CompanyId`** · CRITICAL

**Cặp đơn vị.**
- **Đội A — `Staxi.Admin`, màn "Danh mục điểm đón" (`Landmark`).** Đội A đọc AD-2: "mỗi hãng giữ database riêng", nên hiểu *tenant = database*. `ITenantOwned` với họ có nghĩa "bảng này nằm trong DB của hãng". `ITenantConnectionFactory` đã chọn đúng DB, vậy truy vấn **không cần thêm điều kiện nào nữa**. Repository của đội A trả về mọi dòng trong bảng.
- **Đội B — `Staxi.Admin`, màn "Cấu hình giá cước" (`FareConfig`).** Đội B đọc Conventions: cache key là `{hãng}:{companyId}:{vùng}:{khoá}`, token mang cả `tenant` lẫn `companyId`, nên hiểu *tenant scope = (hãng, công ty)*. `ITenantOwned` với họ có nghĩa `int CompanyId { get; }`, và repository tự thêm `AND CompanyId = @scope`.

**Cả hai tuân thủ.** AD-5 nói "thực thể theo tenant implement `ITenantOwned`" và "truy vấn qua repository tự động thêm điều kiện phạm vi" — nó **không định nghĩa `ITenantOwned` có thuộc tính gì**. AD-2 nói tenant là database. AD-4 và AD-6 nói phạm vi là cặp `tenant` + `companyId`. Spine dùng chữ "tenant" cho cả hai nghĩa, trong cùng một tài liệu.

**Hậu quả.** Hãng G7 có nhiều công ty trong **một** DB (đó chính là lý do web cũ có `ChangeCompany` và cột `CompanyId` ở 215 bảng). Màn hình của đội A hiện điểm đón của **mọi công ty trong hãng** cho người dùng của một công ty. Đó đúng là lỗ hổng `HomeController.ChangeCompany` mà AD-4 tuyên bố ngăn — tái tạo lại bằng cách tuân thủ đúng câu chữ AD-5. Tệ hơn: cả hai màn hình đều xanh CI, vì chốt chắn AD-15 chỉ kiểm "có thực thể **chưa phân loại** hạng tenant", không kiểm phân loại đó **có nghĩa là gì**.

Còn một hệ quả nữa: phân loại ba hạng của AD-5 **thiếu một hạng**. Bảng danh sách công ty của chính hãng đó (`Company`) không phải toàn cục (mỗi DB một nội dung khác), không phải theo tenant (nó không có cột `CompanyId` trỏ tới chính nó theo nghĩa phạm vi), không phải liên kết chéo. Trong 350 bảng chưa rõ hạng, phần lớn sẽ rơi vào hạng thứ tư này: **theo hãng nhưng không theo công ty**. Hai đội gặp một bảng như vậy sẽ gán hai hạng khác nhau, và cả hai đều "đúng".

**AD siết lại — thay thế AD-5:**

> **AD-5 — Phạm vi là cặp `(tenantCode, companyId)`, khai báo bằng kiểu**
> Phạm vi hiệu lực là một kiểu duy nhất `TenantScope { string TenantCode; int CompanyId; }` trong `Staxi.Platform`, chỉ dựng được từ token.
> Mỗi bảng thuộc đúng **một trong bốn** hạng, khai báo bằng attribute trên thực thể và kiểm bằng CI:
> `[GlobalTable]` toàn cục · `[TenantTable]` theo hãng, không theo công ty (lọc bằng chọn DB) · `[CompanyOwned]` theo công ty (**bắt buộc** có `int CompanyId`, repository tự thêm `AND CompanyId = @scope.CompanyId`) · `[CrossTenantLink]` liên kết chéo (xem L-10).
> `ITenantOwned` bị xoá khỏi từ vựng vì tên gọi mơ hồ. Chọn đúng database **không bao giờ** được tính là đã lọc phạm vi.
> Vượt phạm vi công ty trong cùng một DB cũng phải gọi `.AcrossCompanies(lyDo)`; vượt sang DB khác là `.AcrossTenants(lyDo)`. Hai thứ khác nhau, hai quyền khác nhau, hai dòng vết khác nhau.
> **Chốt chắn:** CI đỏ khi một thực thể `[CompanyOwned]` thiếu thuộc tính `CompanyId`, hoặc khi một câu SQL thô chạm bảng `[CompanyOwned]` mà không đi qua repository.

---

## L-02 — AD-4 cấm theo **tên biến**, nên `X-Tenant-Code` đi qua cửa chính · CRITICAL

**Cặp đơn vị.**
- **Đội A — FE `apps/admin-web` + `Staxi.Bff.Web`.** AD-3 bảo nhớ mã hãng trong `localStorage`. Đội A gửi nó lên mọi request bằng header `X-Tenant-Code` để BFF khỏi phải giải mã token cho mỗi lần gom dữ liệu — "tiện, và token vẫn là nguồn xác thực".
- **Đội B — `Staxi.Auth.Api`.** Đội B lấy phạm vi từ claim, không nhận gì từ client.

**Cả hai tuân thủ.** AD-4 viết nguyên văn: "**Cấm nhận `tenantId`/`companyId` từ query, body, header hay form**". Định danh chính thức trong bảng Conventions lại tên là **`tenantCode`**, không phải `tenantId`. Lệnh cấm liệt kê tên, không liệt kê **khái niệm**. Chốt chắn AD-15 cũng không có mục nào cho AD-4. Đội A xanh CI, xanh review.

**Hậu quả.** Một người dùng đã đăng nhập ở hãng A sửa header thành mã hãng B. Nếu BFF (hoặc bất kỳ chỗ nào trên đường đi) dùng header đó để chọn connection, đó là leo thang tenant hoàn chỉnh — **chính xác lỗ hổng AD-4 được viết ra để chặn**, đi lọt vì một chữ.

**AD siết lại — bổ sung vào AD-4:**

> Cấm **mọi giá trị phạm vi** đến từ kênh do client kiểm soát, bất kể tên gọi. Phạm vi chỉ có một nguồn: claim trong token đã ký, đọc qua `ITenantScopeAccessor`.
> **Chốt chắn (thêm vào AD-15):** build đỏ khi (a) một kiểu được model-bind có thuộc tính khớp `(?i)^(tenant|company|hang|cong ?ty)` ; (b) một route template chứa các mẫu đó; (c) có lời gọi `Request.Headers[...]` / `Request.Query[...]` khớp các mẫu đó ngoài adapter `HttpContext` duy nhất.
> Một bài test tích hợp cố định: gửi request kèm `X-Tenant-Code` của hãng khác và khẳng định phản hồi **không đổi**.

---

## L-03 — "UTC ISO-8601" đâm thẳng vào 1.288 chỗ `DateTime.Now` của web cũ · CRITICAL

**Bằng chứng trong repo.** Đếm trên `BA.STaxi.Web`: **1.288 lần `DateTime.Now`** ở 150 file, đối lại **8 lần** `DateTime.UtcNow`/`GETUTCDATE`. Cột `datetime` kế thừa đang chứa **giờ địa phương ICT, không mang offset**. AD-11 nói web cũ **tiếp tục ghi**. AD-10 nói **cấm đổi nghĩa giá trị** của cột — nên không được sửa.

**Cặp đơn vị.**
- **Đội A — `Staxi.Reporting`, "Báo cáo doanh thu theo ngày".** Đọc Conventions: "Lưu và truyền qua API bằng **UTC ISO-8601**; quy đổi sang `vi-VN` **chỉ ở tầng trình bày**". Đội A đọc cột `NgayTao` ra `DateTime`, gắn `Kind = Utc`, serialize `2026-09-17T02:00:00Z`, FE hiển thị 09:00.
- **Đội B — `Staxi.Admin`, màn "Lịch sử thao tác".** Đội B thấy dữ liệu trong DB rõ ràng là giờ Việt Nam, nên coi cột là local, quy đổi sang UTC ở tầng Infrastructure rồi mới trả ra API: `2026-09-16T19:00:00Z`, FE hiển thị 02:00 ngày 17.

**Cả hai tuân thủ.** Convention chỉ nói *API truyền UTC*. Nó **im lặng hoàn toàn về nghĩa của dữ liệu đã có trong cột**. Cả hai đều truyền UTC ISO-8601 và cả hai đều chỉ quy đổi ở trình bày.

**Hậu quả.** Lệch 7 giờ giữa hai màn hình của cùng một hệ thống, và lệch **một ngày** trên mọi báo cáo gom theo ngày — thứ mà một hãng taxi đối soát tiền hằng ngày. Không test đơn vị nào bắt được, vì mỗi đội test theo giả định của chính mình. Thêm một tầng: "ngày kinh doanh" của taxi thường cắt ở 04:00 hoặc theo ca, không phải 00:00 — spine chưa chốt, nên đội A cắt 00:00 ICT, đội B cắt 00:00 UTC, và cả hai lệch với báo cáo web cũ.

**AD mới — AD-16:**

> **AD-16 — Thời gian: cột kế thừa là giờ ICT không offset, biên quy đổi có đúng một chỗ**
> Bất biến kế thừa (không sửa được, do AD-10 và AD-11): mọi cột `datetime` đang có chứa **giờ `Asia/Ho_Chi_Minh`, không offset**.
> Đọc/ghi các cột đó **chỉ** qua kiểu `LegacyLocalTime` ở tầng Infrastructure; nó quy đổi tại biên và là nơi duy nhất biết bất biến trên.
> Cột **mới** (AD-10 cho phép thêm cột `NULL`-able) lưu `datetimeoffset` và luôn ghi UTC; tên cột kết thúc bằng `Utc`.
> API luôn truyền ISO-8601 **có offset**. Trong Domain/Application cấm kiểu `DateTime` trần — dùng `DateTimeOffset` hoặc `LegacyLocalTime`.
> **Ngày kinh doanh** định nghĩa một lần trong `Staxi.Platform.BusinessDay` (mốc cắt + múi giờ), và mọi báo cáo gom theo ngày phải gọi nó, không được tự `CAST(... AS date)`.
> **Chốt chắn:** CI đỏ khi một DTO của Dapper có thuộc tính `DateTime`; khi mã nguồn WEB2 có `DateTime.Now`; khi SQL trong `db/migrations` có `GETDATE()`.

---

## L-04 — Ghi vết của AD-11 chỉ có một nửa, và nửa kia tắt được bằng config · CRITICAL

**Bằng chứng trong repo.** Web cũ **đã có** sổ vết riêng: `BA.STaxi.Web/AuditTrail/AdminAuditTrailHelpers.cs`, `LimoAuditTrailHelpers.cs`, `LogHelpers.cs`. Hình dạng của nó là `(userName, AuditType, AuditTable, oldObject, newObject, note, companyId)` — dùng `CompareNetObjects` để diff hai đối tượng, định danh bảng bằng **enum `AuditTable`**, và **không có `corrId`**. Toàn bộ khối này bị bọc trong `if (!Global.AuditActive) return;`.

**Cặp đơn vị.**
- **Đội A — WEB2 `Staxi.Admin`, sửa hồ sơ tài xế.** Ghi vết theo đúng AD-11: `corrId` + tenant + user + bảng + khoá bản ghi. Không có giá trị cũ/mới (AD-11 không yêu cầu).
- **Đội B — web cũ `DriverController` (vẫn chạy, theo AD-11).** Ghi vết theo `AuditTrailHelper.SaveAudit`: có giá trị cũ/mới, không có `corrId`, tên bảng là một enum không chứa bảng mới của WEB2.

**Cả hai tuân thủ.** AD-11 chỉ ràng buộc WEB2. Nó nói "ghi vết là thứ **duy nhất** khiến mất dữ liệu âm thầm **phát hiện được sau khi xảy ra**" — rồi không kiểm tra giả định đó có đứng vững không.

**Hậu quả — bù đắp của AD-11 không hoạt động:**
1. **Không đối chiếu được.** Vết WEB2 không có giá trị trước/sau, nên khi web cũ ghi đè, vết WEB2 không chứng minh được giá trị nào đã mất. Vết web cũ có diff nhưng không có `corrId` để nối vào.
2. **Hai kho, hai lược đồ.** Khoá bản ghi là `string` ở bên này, bảng là `enum AuditTable` ở bên kia. Một truy vấn pháp chứng sau sự cố phải join tay hai nguồn khác kiểu.
3. **Tắt được.** `Global.AuditActive = false` làm bay toàn bộ nửa web cũ mà không ai biết. Một "bù đắp" mà một dòng config vô hiệu hoá được thì không phải bù đắp.
4. **Enum chặn mở rộng.** WEB2 tạo bảng mới (AD-10 cho phép) → web cũ không có member `AuditTable` tương ứng, nên nếu sau này cần vá web cũ ghi vết cho bảng đó, phải sửa enum, tức sửa web cũ.

**AD siết lại — thay vế thứ hai của AD-11:**

> Ghi vết phải **đủ để dựng lại sự kiện ghi đè**, không chỉ đủ để biết có ai đó ghi. Mỗi dòng vết gồm: `corrId`, `tenantCode`, `companyId`, user, **nguồn** (`web2` | `legacy` | `job` | `sql`), bảng, khoá bản ghi dạng `string`, **giá trị trước và giá trị sau** của các cột đổi, và mốc thời gian UTC.
> Vì web cũ không ghi được hình dạng này, phần phát hiện ghi đè **không được đặt ở tầng ứng dụng**: mỗi bảng `[CompanyOwned]` mà cả hai ứng dụng cùng ghi phải có bộ ba cột `NULL`-able mới `LastWriteSource`, `LastWriteAtUtc`, `LastWriteCorrId` (AD-10 cho phép thêm cột) + **trigger `AFTER UPDATE`** ghi vào bảng vết dùng chung. Trigger là thứ duy nhất nhìn thấy cả hai ứng dụng.
> Sổ vết nằm ở **một** kho, một lược đồ, viết qua **một** `IAuditWriter` trong `Staxi.Platform`. Cấm ghi vết ra log OTel thay cho kho vết.
> Không có cờ tắt ghi vết trong môi trường production.
> **Chốt chắn:** CI đỏ khi một repository có phương thức ghi mà không đi qua `IAuditWriter`; một bài test tích hợp ghi một bản ghi rồi khẳng định có đúng một dòng vết đầy đủ trường.
> Danh sách bảng "cả hai cùng ghi" là một file trong git (xem L-09) và thêm bảng vào đó là điều kiện để bật màn hình WEB2 tương ứng.

---

## L-05 — Dữ liệu "toàn cục" không tồn tại về mặt vật lý, nhưng AD-6 cho phép key dùng chung · CRITICAL

**Cặp đơn vị.**
- **Đội A — `Staxi.Admin`, dropdown "Loại xe" trên màn đăng ký xe.** AD-6 nói nguyên văn: "Dữ liệu **bất biến theo tenant** (tỉnh thành, loại xe) mới được dùng key dùng chung, và phải khai báo tường minh". Đội A khai báo tường minh, đọc bảng `VehicleType` từ DB của hãng đang phục vụ, cache dưới key dùng chung `global:vehicle-type`.
- **Đội B — `Staxi.Reporting`, bộ lọc "Tỉnh thành" trên báo cáo chuyến.** Cũng khai báo tường minh, cũng đọc `Province` từ DB hãng đang phục vụ, cache dưới `global:province`.

**Cả hai tuân thủ.** AD-6 cho phép đúng hai loại dữ liệu này dùng key chung. AD-5 có hạng "toàn cục". Không AD nào nói **bảng toàn cục nằm ở đâu** khi AD-2 quy định 18 database riêng biệt.

**Hậu quả.** Schema là **kế thừa** — mỗi trong 18 DB có bản `Province`/`VehicleType` **của riêng nó**, được nhập liệu độc lập qua nhiều năm. Id không đồng nhất; hãng A có `VehicleType 7 = xe 7 chỗ`, hãng B có `VehicleType 7 = xe VIP`. Request đầu tiên trong ngày tình cờ đến từ hãng A sẽ nạp cache dùng chung; 17 hãng còn lại nhận danh mục của hãng A trong suốt TTL. Người dùng hãng B chọn "xe 7 chỗ" và ghi xuống DB của B một id có nghĩa khác. Đây là **hỏng dữ liệu, không phải chỉ hiển thị sai**, và nó do AD-6 **cho phép một cách tường minh**.

Cùng lỗ đó ở dạng thứ hai: bảng liên kết chéo (AD-5) **không thể tồn tại** khi 18 hãng nằm ở 18 DB — không join được xuyên DB. Đội A đặt bảng liên kết ở DB quản trị (AD-14 không cấm thêm bảng vào đó); đội B nhân bản nó vào từng DB khách. Hai nguồn sự thật cho cùng một quan hệ.

**AD mới — AD-17:**

> **AD-17 — Dữ liệu toàn cục phải có một nơi ở vật lý, hoặc nó không phải toàn cục**
> Một bảng chỉ được xếp hạng "toàn cục" khi nó **nằm ở đúng một nơi**: DB quản trị. Bảng cùng tên tồn tại 18 bản trong 18 DB khách thì **theo định nghĩa là hạng theo hãng** (`[TenantTable]`), dù nội dung nhìn giống nhau.
> Hệ quả bắt buộc: **cấm key cache dùng chung**. Mọi key mang tiền tố `{môi-trường}:{tenantCode}:{companyId}`, không có ngoại lệ, kể cả tỉnh thành và loại xe. Câu "dữ liệu bất biến theo tenant mới được dùng key dùng chung" bị **xoá khỏi AD-6** — lợi ích cache nhỏ hơn rủi ro nhiều bậc.
> Bảng liên kết chéo giữa các hãng chỉ tồn tại ở **DB quản trị**; cấm bản sao trong DB khách. Truy vấn có liên quan phải là hai lần đọc + ghép ở Application, không phải một `JOIN`.
> Nếu về sau thực sự cần một danh mục chuẩn dùng chung (tỉnh thành), đó là một **dự án đồng bộ dữ liệu riêng** kèm bảng ánh xạ id cũ→id chuẩn, không phải một quyết định cache.
> **Chốt chắn:** CI đỏ khi `CacheKey` được dựng bằng bất kỳ overload nào không nhận `TenantScope`.

---

## L-06 — AD-7 tự mâu thuẫn trong chính một câu · CRITICAL

**Nguyên văn AD-7:** "Tenant lấy từ **token lúc handshake**, ghi vào `Context.Items`, và **phân giải lại ở mỗi lần gọi hub method**".

Phân giải lại từ một ảnh chụp tại thời điểm handshake **không phải là phân giải lại**. Đó là đọc lại cùng một giá trị cũ. Câu này cho phép hai cách hiện thực loại trừ nhau.

**Cặp đơn vị.**
- **Đội A — `Staxi.Notification.Api`, hub điều xe.** Hiểu "phân giải lại" = mỗi lần gọi tạo DI scope mới và **đọc lại token hiện tại của connection**, so `permissionVersion` với DB/Redis, sai thì `Abort()`. Khi người dùng đổi công ty, FE của đội A **ngắt và bắt tay lại** bằng token mới.
- **Đội B — `Staxi.Notification.Api`, hub thông báo hệ thống.** Hiểu "phân giải lại" = mỗi lần gọi đọc lại `Context.Items["TenantScope"]` (đúng như câu chữ), và cung cấp hub method `SwitchCompany(int companyId)` để đổi group mà **không ngắt kết nối** — AD-7 chỉ nói "đổi công ty ⇒ rời group cũ, vào group mới", không nói phải bắt tay lại.

**Cả hai tuân thủ.** Đội B tuân thủ **chặt hơn về câu chữ** (dùng đúng `Context.Items`) và lại sai hơn về bản chất.

**Hậu quả.**
1. Kết nối của đội B sống với claim của công ty cũ. `SwitchCompany` nhận `companyId` **từ client** — mà AD-4 chỉ cấm điều đó ở query/body/header/form; một tham số hub method không nằm trong danh sách đó (lỗ này giao với L-02). Leo thang công ty qua SignalR.
2. Kết nối WebSocket sống hàng giờ. Token hết hạn, quyền bị thu hồi, `permissionVersion` tăng — connection của đội B vẫn nhận bản tin. Không AD nào nói kết nối phải chết khi token chết.
3. FE của hai đội hành xử khác nhau khi đổi công ty, nên một màn hình dùng cả hai hub sẽ ở hai công ty cùng lúc.

**AD siết lại — thay vế đầu AD-7:**

> Phạm vi của một lời gọi hub **phải được dựng lại từ token hiện tại của connection ở mỗi lần gọi**, không đọc từ `Context.Items`. `Context.Items` chỉ được dùng để cache **kết quả đã xác thực của chính lần gọi đó**.
> Mỗi lần gọi hub kiểm `permissionVersion` và hạn token; sai hoặc hết hạn ⇒ `Abort()` kết nối. Kết nối cũng bị `Abort()` khi token hết hạn dù không có lời gọi nào (timer).
> **Đổi công ty = ngắt kết nối và bắt tay lại bằng token mới.** Cấm mọi hub method nhận `companyId`/`tenantCode` làm tham số; cấm hub method đổi phạm vi.
> **Chốt chắn (thêm vào AD-15):** build đỏ khi một phương thức của `Hub` có tham số khớp `(?i)(tenant|company)`, hoặc khi có `Context.Items` được đọc ngoài lớp bọc `TenantScope`.

---

## L-07 — Không ai bump `permissionVersion` khi **web cũ** đổi quyền · CRITICAL

**Cặp đơn vị.**
- **Đội A — WEB2 `Staxi.Auth`, màn "Phân quyền người dùng" (đã chuyển).** Đổi vai trò ⇒ tăng `permissionVersion` trong DB ⇒ token cũ bị từ chối ở request kế tiếp.
- **Đội B — web cũ `AdminUserController`, cũng là màn phân quyền, **vẫn chạy** theo AD-11.** Admin thu hồi quyền của một nhân viên vừa nghỉ việc. Web cũ ghi thẳng bảng quyền. Nó **không biết** cột/khái niệm `permissionVersion` tồn tại — mà nó không cần biết, vì AD-10 cấm đổi nghĩa cột và không có AD nào bắt sửa web cũ.

**Cả hai tuân thủ.** AD-4 nói token mang `permissionVersion`; Conventions nói "kiểm lại mỗi request". Không AD nào nói **ai có nghĩa vụ tăng nó**, và AD-11 nói rõ web cũ được ghi tự do.

**Hậu quả.** Quyền bị thu hồi ở web cũ **không** vô hiệu hoá token WEB2. Với token 8 giờ (không AD nào chốt thời hạn), nhân viên đã nghỉ giữ nguyên quyền trên WEB2 đến hết ca. Đây là lỗ hổng an ninh do chính chiến lược song song sinh ra, và spine không có một dòng nào về nó. Thêm nữa, "kiểm lại mỗi request" so với **cái gì** cũng không chốt: đội A so với giá trị cache Redis TTL 60 giây (hợp lệ theo AD-6), đội B so với một lần đọc DB mỗi request. Hai độ trễ thu hồi khác nhau trong cùng một hệ.

**AD siết lại — bổ sung vào AD-4:**

> `permissionVersion` và `SecurityStamp` được tăng bởi **trigger trên các bảng quyền trong DB**, không phải bởi mã ứng dụng — vì web cũ cũng ghi các bảng đó và không thể sửa nó.
> Thời hạn access token ≤ **15 phút**; làm mới bằng refresh token có kiểm `permissionVersion` tại thời điểm làm mới.
> Kiểm `permissionVersion` mỗi request đọc qua **một** `IPermissionVersionProvider`; cho phép cache nhưng **độ trễ thu hồi tối đa 5 giây**, đạt được bằng read-through + pub/sub thu hồi, không bằng TTL.
> Một bài test tích hợp: thu hồi quyền **bằng câu SQL trần** (mô phỏng web cũ) rồi khẳng định request WEB2 kế tiếp bị từ chối trong 5 giây.

---

## L-08 — Proc vừa gom vừa ghi rơi đúng vào khe giữa hai vế của AD-9 · CRITICAL

**Cặp đơn vị.**
- **Đội A — `Staxi.Admin`, "Sửa chuyến".** AD-9 vế một: "chuyển sang .NET cái gì đọc–ghi theo thực thể". Đội A viết repository Dapper, đi qua `TenantScope`, ghi vết qua `IAuditWriter`, có `[CompanyOwned]`.
- **Đội B — `Staxi.Reporting`, "Chốt ca / tổng hợp doanh thu".** Proc kế thừa `sp_ChotCa` có `GROUP BY`, temp table và window function — AD-9 vế hai nói rõ: "**giữ trong SQL** cái gì gom dữ liệu... và bọc bằng Dapper trả DTO". Đội B giữ nguyên. Nhưng proc đó, như hầu hết proc chốt ca trong hệ điều vận, **vừa gom vừa `UPDATE` trạng thái chuyến và `INSERT` dòng đối soát**.

**Cả hai tuân thủ.** AD-9 phân loại theo **hình dạng truy vấn** (gom hay không gom), còn thế giới thật phân loại theo **tác dụng phụ** (ghi hay không ghi). Một proc vừa gom vừa ghi thoả cả hai vế, và đội B chọn vế thuận tiện hơn — hợp lệ theo câu chữ, và cũng là lựa chọn rẻ hơn nên sẽ luôn thắng.

**Hậu quả.**
1. **Ghi không có vết.** `IAuditWriter` sống ở .NET; proc ghi thẳng. Một nửa số lần ghi vào `Trip` có vết, một nửa vô hình — làm hỏng nốt phần còn lại của bù đắp AD-11 (xem L-04).
2. **Ghi không có lọc phạm vi.** Repository tự thêm `AND CompanyId`; proc nhận `@CompanyId` làm tham số và tin nó. Nếu tham số đó đi từ BFF xuống, ta lại rơi vào L-02.
3. **Chốt chắn AD-15 mù.** Mọi kiểm tra của AD-15 là quét assembly. Không mục nào nhìn được vào chuỗi SQL, nói gì đến thân proc. Với 2.003 proc, đây là vùng tối lớn nhất của hệ.

**AD siết lại — thay tiêu chí phân loại của AD-9:**

> Tiêu chí phân loại là **tác dụng phụ**, không phải hình dạng truy vấn.
> Proc **chỉ đọc** (kể cả gom nặng): giữ trong SQL, bọc Dapper trả DTO. Đây là vế duy nhất được giữ lại tự do.
> Proc **có ghi** (`INSERT`/`UPDATE`/`DELETE`/`MERGE`, trực tiếp hay gián tiếp): phải **tách đôi** — phần gom ở lại SQL, phần ghi lên .NET đi qua repository và `IAuditWriter`. Khi chưa tách kịp, proc đó phải được đăng ký trong `db/write-procs.json` kèm: chủ sở hữu, bảng nó ghi, khoá bản ghi, ngày hết hạn, và được gọi qua `IWriteProcedure` — lớp này sinh dòng vết **trong cùng transaction** với lời gọi proc.
> **Chốt chắn:** CI đỏ khi một chuỗi SQL truyền cho Dapper khớp `(?i)\b(insert|update|delete|merge|exec)\b` mà lời gọi không nằm trong `IWriteProcedure`, hoặc proc không có trong `write-procs.json`. Một job quét `sys.sql_modules` của 18 DB, đối chiếu danh sách proc-có-ghi với file đăng ký và **làm đỏ build** khi lệch.

---

## L-09 — Không có sổ chủ sở hữu màn hình: Strangler Fig không có bước siết · HIGH

**Cặp đơn vị.**
- **Đội A** chuyển màn "Quản lý tài xế" sang WEB2, xong, bật cho hãng 1.
- **Đội B** hai tháng sau chuyển màn "Duyệt hồ sơ tài xế" sang WEB2. Đồng thời, điều phối viên vẫn dùng màn "Quản lý tài xế" **của web cũ** vì nó vẫn ở đó, vẫn chạy, và quen tay hơn.

**Cả hai tuân thủ.** Paradigm viết: "Từng màn hình chuyển sang khi sẵn sàng; web cũ **không bị đụng tới** và tiếp tục chạy cho tới khi rỗng". Không AD nào nói màn hình đã chuyển thì **phải bị chặn ở web cũ**. Với "không đụng tới web cũ" là nguyên tắc, chặn lại có vẻ là vi phạm.

**Hậu quả.** Strangler Fig không có bước siết thì không phải strangler — nó là hai hệ thống sinh đôi chạy vĩnh viễn. Cửa sổ ghi đè last-write-wins (AD-11) không phải vài tuần chuyển tiếp mà là **trạng thái thường trực**, trên chính những màn hình quan trọng nhất. Và web cũ sẽ **không bao giờ "rỗng"**, vì không có định nghĩa nào cho "đã chuyển".

**AD mới — AD-18:**

> **AD-18 — Sổ chủ sở hữu màn hình, và siết ngay khi chuyển**
> `db/screen-ownership.json` là nguồn sự thật: mỗi màn hình ↔ danh sách controller/action của web cũ ↔ bảng nó ghi ↔ trạng thái `legacy` | `dual` | `web2` ↔ ngày hết hạn của trạng thái `dual` (tối đa **30 ngày**).
> Bật một màn hình WEB2 cho một hãng ⇒ trong cùng đợt phát hành, action tương ứng của web cũ chuyển sang **chỉ-đọc** cho hãng đó (một filter đọc sổ này), và sau thời hạn `dual` thì **chặn hẳn kèm liên kết chuyển sang WEB2**. Đó là sửa web cũ có kiểm soát, và nó rẻ hơn nhiều so với mất dữ liệu.
> Trạng thái `dual` là ngoại lệ có hạn dùng, phải có người ký tên, không phải mặc định.
> Tập bảng "cả hai cùng ghi" của L-04 được **sinh ra** từ file này, không khai tay.
> **Chốt chắn:** CI đỏ khi một trạng thái `dual` quá hạn, hoặc khi một màn hình WEB2 chạm bảng chưa có trong sổ.

---

## L-10 — Bảng liên kết chéo không định nghĩa vị từ phạm vi · HIGH

**Cặp đơn vị.** Bảng `CompanyPartner(CompanyIdA, CompanyIdB, ...)` cho điều xe liên công ty.
- **Đội A — màn "Quản lý đối tác".** Hiểu phạm vi theo **chủ sở hữu**: `WHERE CompanyIdA = @scope`. Bạn quản lý những liên kết bạn tạo ra.
- **Đội B — "Báo cáo chuyến liên công ty".** Hiểu phạm vi theo **bên tham gia**: `WHERE CompanyIdA = @scope OR CompanyIdB = @scope`. Bạn thấy những liên kết bạn có mặt trong đó.

**Cả hai tuân thủ.** AD-5 công nhận hạng "liên kết chéo" và dừng lại ở đó. Không AD nào nói vị từ phạm vi của hạng này là gì, cũng không nói nó **có hướng** hay **đối xứng**. Không bên nào cần `.AcrossTenants(lyDo)` vì cả hai đều tin mình đang ở trong phạm vi — nên cũng không có dòng vết nào.

**Hậu quả.** Công ty đối tác thấy chuyến trong báo cáo nhưng không thấy liên kết trong màn quản lý, nên không sửa/không huỷ được. Ngược lại, đội A xoá một dòng và báo cáo của đội B im lặng mất dữ liệu — không phải lỗi, chỉ là hai định nghĩa. Cộng với L-05 (bảng này nằm ở đâu?) thì thành hai nguồn sự thật với hai vị từ.

**AD siết lại — bổ sung vào AD-5:**

> Mỗi bảng `[CrossTenantLink]` phải khai báo **một** vị từ phạm vi, đúng một chỗ, bằng một hiện thực `ICrossLinkScope` trong Domain, và khai `Directed` hay `Symmetric`. Repository chỉ dựng câu lệnh từ vị từ đó.
> Đọc ngoài vị từ đã khai ⇒ bắt buộc `.AcrossTenants(lyDo)`.
> Xoá một dòng liên kết là thao tác ảnh hưởng **cả hai** bên: phải là xoá mềm + ghi vết cho cả hai `companyId`.
> **Chốt chắn:** CI đỏ khi một bảng `[CrossTenantLink]` không có `ICrossLinkScope` tương ứng, hoặc khi có SQL thô chạm bảng đó.

---

## L-11 — `{vùng}` và hình dạng giá trị cache không có sổ đăng ký · HIGH

**Cặp đơn vị.**
- **Đội A — BFF màn "Điều xe".** Key `g7:12:driver:list`, giá trị là `DriverListItem { Id, Name, Status }`, TTL 60 giây, xoá khi có ghi.
- **Đội B — BFF màn "Danh bạ tài xế".** Key `g7:12:driver:list` (cùng vùng "driver", cùng khoá "list" — hai đội gọi cùng một thứ bằng cùng một cái tên, hoàn toàn tự nhiên), giá trị là `DriverContact { Id, Name, Phone, LicenseNo }`, TTL 600 giây, không xoá khi ghi.

**Cả hai tuân thủ.** AD-6 ràng buộc **tiền tố** (hãng, companyId) và nói key mang tem `permissionVersion` khi phụ thuộc quyền. `{vùng}` và `{khoá}` hoàn toàn tự do, không có sổ đăng ký, không có phiên bản hình dạng giá trị, không có luật TTL, không có luật thu hồi.

**Hậu quả.** Cùng key, hai kiểu ⇒ hoặc ngoại lệ deserialize (may mắn, nổ ngay) hoặc **mất trường im lặng** (JSON thừa/thiếu field — kịch bản thường gặp hơn): màn điều xe hiển thị số điện thoại rỗng, hoặc màn danh bạ hiện danh sách thiếu người. Và vì đội B không thu hồi cache khi ghi, sửa tài xế ở màn A vẫn hiện cũ ở màn B tới 10 phút — một lớp đọc-cũ chồng lên nền last-write-wins vốn đã không có bảo vệ tương tranh, khiến người dùng ghi đè dựa trên dữ liệu họ nhìn thấy đã cũ.

**AD siết lại — bổ sung vào AD-6:**

> `{vùng}` lấy từ một **enum đóng** trong `Staxi.Platform`; mỗi vùng có **đúng một module chủ sở hữu**, khai trong `cache-regions.json` (vùng → assembly chủ → kiểu giá trị → TTL tối đa). Chỉ module chủ được **ghi** vào vùng của mình; module khác chỉ đọc.
> Key mang **phiên bản hình dạng** giá trị: `...:{vùng}:v{n}:{khoá}`; đổi kiểu giá trị ⇒ tăng `n`, không bao giờ tái dùng key cũ.
> Thu hồi là nghĩa vụ của module chủ: mọi lệnh ghi vào thực thể của vùng đó phải publish thu hồi. Vùng nào không thu hồi được thì TTL ≤ 30 giây.
> **Chốt chắn:** CI đỏ khi hai assembly cùng ghi một vùng, khi một vùng thiếu đăng ký, hoặc khi kiểu giá trị lệch với đăng ký.

---

## L-12 — Tem `permissionVersion` phán đoán bởi bên ghi, tiêu thụ bởi bên đọc · HIGH

**Cặp đơn vị.**
- **Đội A — `Staxi.Admin` nạp cache `g7:12:driver:list`** = **toàn bộ** tài xế của công ty. Đội A lập luận đúng theo AD-6: nội dung này **không** phụ thuộc quyền — nó là toàn bộ tập, giống nhau với mọi người dùng. Không đóng tem `permissionVersion`. Hợp lệ.
- **Đội B — BFF màn "Điều xe theo đội"**, đọc lại đúng key đó rồi hiển thị. Người dùng của đội B chỉ được xem tài xế thuộc đội của mình; đội B định lọc ở BFF nhưng (xem L-16) coi việc lọc là nghiệp vụ nên đẩy xuống API — API lại đọc từ cache đã gom sẵn của đội A.

**Cả hai tuân thủ.** AD-6 để việc phán đoán "nội dung có phụ thuộc quyền không" cho **bên ghi cache**, trong khi hậu quả thuộc về **bên đọc**. Với hai đội, hai thời điểm, phán đoán đó chắc chắn lệch.

**Hậu quả.** Rò dữ liệu theo quyền trong cùng một công ty — nhẹ hơn rò xuyên tenant nhưng vẫn là rò, và không có chốt chắn nào bắt được vì `CacheKey` **có** tenant nên AD-15 thấy hợp lệ.

**AD siết lại:** phán đoán chuyển từ bên ghi sang **kiểu dữ liệu**. Mỗi vùng cache khai trong `cache-regions.json` là `PermissionInvariant` hay `PermissionScoped`; vùng `PermissionScoped` thì `CacheKey` **không dựng được** nếu thiếu `permissionVersion` (giống cách AD-6 làm với tenant). Cấm một module đọc vùng do module khác sở hữu nếu hai bên khác hạng quyền.

---

## L-13 — `{chủ-đề}` SignalR tự do → trùng tên group, hai payload · HIGH

**Cặp đơn vị.**
- **Đội A — điều vận.** Group `g7:12:trip`, bắn sự kiện `TripStatusChanged { tripId, status }` bằng phương thức client `onTrip`.
- **Đội B — giám sát.** Group `g7:12:trip`, bắn `TripPositionUpdated { tripId, lat, lng }` bằng phương thức client `receiveTrip`.

**Cả hai tuân thủ.** AD-7 bắt tên group sinh bởi "một hàm duy nhất" theo khuôn `{hãng}:{companyId}:{chủ-đề}` — hàm đó đảm bảo **định dạng**, không đảm bảo **tính duy nhất của `{chủ-đề}`**. Tên phương thức phía client không được nhắc đến ở bất cứ đâu trong spine.

**Hậu quả.** Client đăng ký một luồng nhận cả hai loại bản tin; FE hoặc nổ, hoặc render sai. Việc gỡ lỗi rất đắt vì SignalR không báo lỗi — bản tin chỉ đơn giản đến nơi không mong đợi (đúng mô tả "lỗi không để lại log" mà AD-7 tự nêu ở phần *Prevents*).

**AD siết lại — bổ sung vào AD-7:** `{chủ-đề}` lấy từ **enum đóng** trong `Staxi.Platform`; mỗi chủ đề ràng buộc với **đúng một kiểu payload** và **đúng một tên phương thức client**, khai trong một bảng đăng ký sinh ra cả hằng TypeScript cho FE. CI đỏ khi có chuỗi group dựng bằng nối chuỗi, khi hai kiểu payload cùng chủ đề, hoặc khi hằng FE lệch bảng đăng ký.

---

## L-14 — Mã quyền trôi dạt, CI vẫn xanh · HIGH

**Cặp đơn vị.**
- **Đội A** khai `[Permission("Driver.Edit")]` — đặt tên theo lối mới, gọn.
- **Đội B** khai `[Permission("QuanLyTaiXe_Sua")]` — dùng đúng mã đang có trong `PermissionTree.cs` và bảng quyền của web cũ, vì người dùng được cấp quyền bằng màn phân quyền của web cũ (AD-11 cho phép web cũ chạy tiếp).

**Cả hai tuân thủ.** AD-15 chỉ kiểm "có action **thiếu** khai báo quyền" — tức là **có hay không**, không phải **có đúng mã hay không**. Bảng paradigm nhắc tới "sổ đăng ký quyền" ở tầng Application nhưng không AD nào biến nó thành nguồn sự thật duy nhất, và không AD nào nối nó với cây quyền kế thừa.

**Hậu quả.** Quyền của đội A không tồn tại trong dữ liệu ⇒ hoặc không ai dùng được màn hình, hoặc (tệ hơn) hiện thực fallback "không tìm thấy quyền ⇒ cho qua". Người quản trị nhìn thấy hai hệ mã quyền cho cùng một năng lực, cấp quyền ở web cũ mà WEB2 không nhận. Với thời gian, ánh xạ giữa hai hệ trở thành tri thức truyền miệng.

**AD mới — AD-19:**

> **AD-19 — Sổ đăng ký quyền duy nhất, neo vào mã quyền kế thừa**
> Mọi mã quyền nằm trong `permissions.json`, sinh ra hằng C# và hằng TypeScript. Mỗi mục **bắt buộc** ánh xạ 1-1 tới mã trong `PermissionTree.cs` của web cũ, hoặc đánh dấu `new: true` kèm script cấp phát vào bảng quyền của 18 DB.
> Điểm cưỡng chế quyền là **API nghiệp vụ**, luôn luôn. BFF được lọc trước cho trải nghiệm nhưng **không bao giờ là chốt duy nhất** (giao với AD-12).
> Không có fallback cho phép khi thiếu quyền; thiếu là từ chối.
> **Chốt chắn:** CI đỏ khi một chuỗi quyền trong mã không có trong `permissions.json`; khi một mục thiếu ánh xạ kế thừa; khi một action của API nghiệp vụ không có attribute quyền (mở rộng mục sẵn có của AD-15 từ "có khai báo" thành "khai báo hợp lệ").

---

## L-15 — Không có kiểu dùng chung → `status: 2` và `status: "DangCho"` cùng một chuyến · HIGH

**Cặp đơn vị.**
- **Đội A — `Staxi.Admin.Domain`** định nghĩa `enum TripStatus { Cho = 1, DangCho = 2, HoanThanh = 3 }`, serialize ra JSON **theo số** (mặc định của `System.Text.Json`).
- **Đội B — `Staxi.Reporting.Application`** định nghĩa `enum TrangThaiChuyen` ánh xạ cột `varchar` `'DC'`/`'HT'` (schema kế thừa có cả hai cách biểu diễn cho cùng khái niệm, ở hai bảng), serialize **theo chuỗi**.

**Cả hai tuân thủ.** Clean Architecture đặt Domain **bên trong mỗi dịch vụ**; cây nguồn mô tả `Staxi.Platform` là "lõi dùng chung: TenantContext, CacheKey, ProblemDetails, tracing" — **không có chỗ cho từ vựng nghiệp vụ chung**, và spine không cấm mỗi dịch vụ tự định nghĩa. Conventions chốt định danh và ngày/số nhưng **im lặng về enum và về JSON casing** (xem L-24).

**Hậu quả.** FE nhận `status: 2` từ endpoint này và `status: "DangCho"` từ endpoint kia cho cùng một chuyến, nên phải viết lớp chuẩn hoá ở FE — tức là **nghiệp vụ mọc ở FE**, đúng thứ AD-12 muốn tránh, nhưng mọc ở chỗ AD-12 không nhìn tới. Khi thêm một trạng thái mới, hai enum lệch nhau âm thầm.

**AD mới — AD-20:**

> **AD-20 — Từ vựng chung nằm trong `Staxi.Contracts`, hình dạng dây chốt cứng**
> Khái niệm xuất hiện ở **hơn một dịch vụ** (trạng thái chuyến, trạng thái tài xế, loại thanh toán, mã lỗi nghiệp vụ) sống trong assembly `Staxi.Contracts`, cùng với **bảng ánh xạ** từ mọi biểu diễn kế thừa (`int`, `varchar`) sang một enum chuẩn. Ánh xạ là mã, không phải tri thức.
> Trên dây: enum luôn là **chuỗi `PascalCase`**; cấm `JsonNumberEnumConverter`. Thuộc tính JSON là `camelCase`. Tiền là `decimal` serialize dạng chuỗi. Id giữ kiểu DB nhưng **luôn** serialize dạng chuỗi.
> Kiểu của `Staxi.Contracts` sinh ra định nghĩa TypeScript cho FE trong cùng bước build.
> **Chốt chắn:** CI đỏ khi hai assembly khai enum cùng tên hoặc cùng tập member; khi một DTO công khai lộ enum mà không qua `Staxi.Contracts`; khi định nghĩa TS sinh ra lệch với bản đã commit.

---

## L-16 — Ranh giới "gom dữ liệu" vs "nghiệp vụ" của AD-12 phân định được hai cách · HIGH

**Cặp đơn vị.** Cùng một màn hình: "Bảng điều khiển ca trực", cần hiện huy hiệu **"tài xế quá giờ"** = `now - shiftStart > ngưỡng cấu hình`.
- **Đội A** tính trong BFF: nó đã có `shiftStart` trong dữ liệu gom được, chỉ là một phép so sánh để **định hình dữ liệu cho màn hình** — đúng chữ "chiếu và định hình" của AD-12. Ngưỡng đọc từ config của BFF.
- **Đội B**, màn "Cảnh báo vi phạm giờ lái", tính trong `Staxi.Admin.Api` vì đó là quy tắc nghiệp vụ. Ngưỡng đọc từ bảng cấu hình theo công ty.

**Cả hai tuân thủ.** AD-12 nói BFF "**chỉ** gom, chiếu và định hình dữ liệu cho từng màn hình — **cấm** chứa quy tắc nghiệp vụ". "Chiếu và định hình" và "quy tắc nghiệp vụ" không có đường biên kiểm chứng được. Mọi lập trình viên sẽ vẽ nó ở chỗ thuận tay, và trong một dự án tính KPI theo tốc độ, chỗ thuận tay luôn là BFF.

**Hậu quả.** Cùng một khái niệm "quá giờ" cho hai con số khác nhau trên hai màn hình, vì hai nguồn ngưỡng. Khi ngưỡng đổi theo hợp đồng của một hãng, một chỗ được sửa. Ngoài ra: phân trang/sắp xếp cũng rơi vào khe này — đội A lấy 1.000 dòng rồi cắt ở BFF (đúng thứ *Prevents* của AD-9 cảnh báo), đội B đẩy xuống API.

**AD siết lại — thay vế hai của AD-12 bằng danh sách đóng:**

> BFF **chỉ được** làm đúng bốn việc: chọn trường, đổi tên trường, ghép nhiều phản hồi theo khoá, làm phẳng/lồng lại cấu trúc. Mọi thứ khác là nghiệp vụ.
> Cụ thể, trong BFF **cấm**: điều kiện rẽ nhánh trên giá trị nghiệp vụ; phép toán số học ngoài định dạng hiển thị; so sánh với ngưỡng/cấu hình; đọc `SettingManager`/config nghiệp vụ; lọc, sắp xếp, phân trang (phải đẩy xuống API và trả kèm tổng số); tổng hợp.
> Giá trị dẫn xuất mà màn hình cần thì **API trả thẳng ra** (ví dụ `isOverShift: true`), không phải BFF tự tính.
> **Chốt chắn:** CI đỏ khi assembly `Staxi.Bff.*` tham chiếu bất kỳ package dữ liệu/cấu hình nào; khi nó tham chiếu `Staxi.*.Domain`; một bài test cấu trúc kiểm không có `IConfiguration` nào được inject vào handler của BFF.

---

## L-17 — Không nói ai sinh `corrId`, nên một màn hình có ba · HIGH

**Cặp đơn vị.**
- **Đội Gateway** sinh `corrId` tại YARP cho mỗi request vào, truyền tiếp bằng header `X-Correlation-Id`.
- **Đội BFF** sinh `corrId` **của riêng nó** cho mỗi lời gọi xuống API nghiệp vụ, vì AD-8 nói "**mọi** phản hồi mang header `corrId`" — nên mỗi phản hồi cần một cái, và tự sinh là cách chắc chắn nhất để không thiếu.

**Cả hai tuân thủ.** AD-8 quy định `corrId` **phải có mặt**; AD-13 nói nó là mã riêng, không nối với web cũ. Không dòng nào nói **ai sinh**, **sinh mấy lần**, hay **tên header là gì**.

**Hậu quả.** Một màn hình một lời gọi BFF → hai lời gọi API → bốn `corrId` khác nhau. Người dùng đọc cho CSKH cái `corrId` mà FE nhận được (của BFF). Dòng ghi vết của AD-11 lại mang `corrId` sinh ở API. **Không tra ngược được** — nghĩa là bù đắp duy nhất của AD-11 đứt đúng chỗ nó cần liền mạch nhất.

**AD siết lại — bổ sung vào AD-8:** `corrId` sinh **đúng một lần**, tại gateway (FE gửi lên thì gateway chấp nhận nếu hợp lệ). Tên header cố định `X-Correlation-Id`. Mọi chặng phía sau **truyền nguyên**, cấm sinh mới; lan truyền cả qua SignalR và qua job nền. Dòng ghi vết, ProblemDetails, log OTel và màn lỗi FE dùng **cùng một giá trị**. Chốt chắn: một bài test đầu-cuối khẳng định một request qua gateway → BFF → 2 API → 1 dòng vết chỉ có **một** `corrId` duy nhất.

---

## L-18 — grate: Once sửa được sau khi chạy; lệnh cấm đổi hình dạng proc không có cách phát hiện · HIGH

**Cặp đơn vị.**
- **Đội A** thêm `db/migrations/Once/0012_them_cot_driver.sql`, merge vào TESTALL, grate chạy trên devtest, phát hiện sai kiểu, **sửa lại chính file đó** rồi merge tiếp. grate băm nội dung Once script và **báo lỗi khi nội dung đã chạy bị đổi** ⇒ toàn bộ devtest kẹt, và khi lên staging thì 18 DB dừng giữa chừng.
- **Đội B** sửa `db/migrations/AnyTime/sp_BaoCaoDoanhThu.sql`, thêm hai cột vào `SELECT` cuối. AD-10 cấm "sửa proc web cũ đang gọi theo cách đổi hình dạng kết quả" — nhưng **không ai biết web cũ gọi proc nào**: 2.003 proc, không có bản kiểm kê, và web cũ ánh xạ kết quả bằng tên cột lẫn theo chỉ số tuỳ chỗ.

**Cả hai tuân thủ.** AD-10 không nói Once script là bất biến sau merge. Nó cấm đổi hình dạng proc nhưng **không cung cấp cơ chế phát hiện**, nên lệnh cấm chỉ là lời khuyên (xem L-20). Thêm nữa, hai đội đặt tên `0012_*` trên hai nhánh khác nhau — git merge sạch, và thứ tự chạy trở thành thứ tự từ điển của hai tên không ai định trước.

**Hậu quả.** Migration kẹt giữa chừng trên một tập con của 18 DB (xem L-19), và một báo cáo của web cũ vỡ trong production mà không đợt review nào bắt được.

**AD siết lại — bổ sung vào AD-10:**

> Script `Once` là **bất biến sau khi merge**. Sửa chữa đi bằng một script mới. Tên file là `{ticket}_{utc-timestamp}_{slug}.sql`, grate chạy theo timestamp — hai nhánh không thể đụng số.
> `db/legacy-proc-usage.txt` được **sinh tự động** bằng cách quét mã nguồn `BA.STaxi.Web` + `BA.STaxi.LandingPage` tìm tên proc, và commit vào git. Mọi script `AnyTime` chạm một proc có trong file đó phải kèm **chữ ký tập kết quả** trước/sau (tên + kiểu cột) trong cùng PR.
> **Chốt chắn:** CI đỏ khi nội dung một file `Once` đã có trong lịch sử git bị đổi; khi một `AnyTime` chạm proc kế thừa mà chữ ký tập kết quả đổi và không có phê duyệt tường minh; khi một script có `ALTER TABLE ... DROP/ALTER COLUMN` hoặc `sp_rename`.

---

## L-19 — 18 DB lệch mức schema giữa chừng, không có luật chịu lỗi · HIGH

**Cặp đơn vị.** Đợt phát hành thêm một cột `NULL`-able (hợp lệ theo AD-10). grate chạy tuần tự 18 DB; DB số 7 lỗi (timeout, khoá, hoặc hãng đó đang bảo trì).
- **Đội A** viết code đọc cột mới trực tiếp, giả định migration đã chạy — hợp lý, vì migration nằm cùng đợt phát hành.
- **Đội B** viết code dò schema rồi mới đọc.

**Cả hai tuân thủ.** AD-10 nói schema tương thích ngược; Deferred #1 hoãn cơ chế lệch phiên bản DB với lý do "đợt đầu chỉ có vài màn hình". Nhưng lệch phiên bản **không đợi hãng thứ hai lên WEB2** — nó xảy ra ngay lần đầu một migration chạy không trọn 18 DB. Điều kiện xem lại trong bảng Deferred đặt sai mốc.

**Hậu quả.** Code của đội A đổ lỗi 500 cho hãng số 7 trở đi. Không có health check nào biết "18 DB có cùng mức schema không". Và vì WEB2 là **một** bản triển khai cho mọi hãng, không rollback riêng cho một hãng được.

**AD siết lại — bổ sung vào AD-10:**

> Mức schema của mỗi DB được ghi vào bảng sổ cái của grate và **được đọc ra làm health check**: `/health/schema` báo DB nào lệch mức mục tiêu.
> Phát hành theo cổng: **mã dùng cột/bảng mới chỉ được bật khi cả 18 DB đạt mức mục tiêu**, kiểm bằng cờ tính năng đọc từ mức schema, không phải bằng niềm tin vào thứ tự deploy.
> Chạy grate là **all-or-nothing trên toàn 18 DB**: một DB lỗi ⇒ đợt phát hành dừng, không phát hành mã đi kèm.
> Mốc xem lại của Deferred #1 sửa thành: **trước đợt phát hành đầu tiên có DDL**, không phải khi hãng thứ hai lên WEB2.

---

## L-20 — Meta: AD-15 cưỡng chế 7 điều trong khoảng 20 lệnh cấm · HIGH

Đếm các lệnh cấm trong spine: AD-2 (`new SqlConnection`, connection string từ config) · AD-3 (endpoint liệt kê hãng; thông báo lỗi phân biệt) · AD-4 (nhận phạm vi từ client; đổi công ty bằng header) · AD-5 (vượt phạm vi không tường minh) · AD-6 (`CacheKey` thiếu tenant; key chung ngoài danh sách) · AD-7 (`Clients.All/Others/AllExcept`) · AD-8 (**HTTP 200 cho một lỗi**) · AD-9 (dynamic SQL; gom trong RAM) · AD-10 (đổi tên/kiểu/xoá cột; đổi hình dạng proc) · AD-11 (ghi mà không ghi vết) · AD-12 (nghiệp vụ trong BFF; BFF chạm DB; FE gọi thẳng API) · AD-13 (log PII) · Conventions (bí mật trong git; ép culture toàn tiến trình).

AD-15 cưỡng chế **bảy**: static thay đổi được · `HttpContext` ngoài adapter · thực thể chưa phân hạng · action thiếu quyền · `CacheKey` thiếu tenant · `Clients.All` · `new SqlConnection`.

Phần còn lại là **lời khuyên**. Và chính AD-15 tự nêu *Prevents*: "mọi AD ở trên thoái hoá thành lời khuyên" — nó đúng về chẩn đoán và thiếu liều về thuốc. Đáng chú ý nhất, **AD-8 — lệnh cấm quan trọng nhất của spine, sinh ra từ bài học đau nhất của web cũ (HTTP 200 kèm trang lỗi) — không có một chốt chắn nào**.

**Cặp đơn vị cụ thể cho riêng AD-8.** Đội A cấu hình middleware xử lý ngoại lệ trả ProblemDetails đúng mã. Đội B, ở một endpoint tải file, bắt ngoại lệ và trả `Ok(new { success = false, message = ... })` vì "FE của tôi xử lý cờ `success` rồi". Cả hai xanh CI. FE của đội A không hiểu cờ đó, coi là thành công, và ta tái tạo lại **nguyên si** lỗi kinh điển của web cũ, bên trong hệ thống được thiết kế để không lặp lại nó.

**AD siết lại — mở rộng AD-15:**

> Mỗi lệnh cấm trong spine phải kèm **cơ chế phát hiện** ngay trong chính AD đó (test cấu trúc, test tích hợp, quét SQL, hoặc kiểm tra tạo tác build). Một AD không có cơ chế phát hiện **không được nhận trạng thái `accepted`** — nó ở lại `proposed`.
> Bổ sung ngay các chốt chắn: AD-8 (test tích hợp bắn mọi loại lỗi qua mọi endpoint và khẳng định mã ≠ 200 + body là ProblemDetails + có `corrId`; và test cấu trúc cấm `Ok(...)` trong khối `catch`) · AD-4 (L-02) · AD-11 (L-04) · AD-12 (L-16) · AD-9 (L-08) · AD-10 (L-18).
> Bảng "AD ↔ chốt chắn" là một mục bắt buộc trong spine, và ô trống là lỗi review.

---

## L-21 — Redis chung cho cache và backplane, hai tiền tố, thiếu môi trường · MEDIUM

Sơ đồ topo vẽ **một** Redis `R` cho cả backplane lẫn cache. AD-7 quy định backplane có tiền tố `{môi-trường}:{hãng}`. AD-6 quy định cache key là `{hãng}:{companyId}:{vùng}:{khoá}` — **không có `{môi-trường}`**.

**Cặp:** đội DevOps trỏ staging và một máy dev chung một instance Redis (rất thường gặp, và không AD nào cấm). Backplane không đụng nhau nhờ tiền tố môi trường; **cache thì đụng** — dev đọc phải cache của staging và ngược lại. Thêm: một lệnh dọn dẹp `FLUSHDB` từ phía cache **cũng xoá backplane**.

**AD siết lại:** một tiền tố thống nhất `{môi-trường}:{tenantCode}:...` cho **mọi** khoá Redis, cache và backplane như nhau (khớp với AD-17). Backplane dùng **instance hoặc logical DB riêng** với cache. Cấm mọi lệnh `FLUSH*`/`KEYS` trong mã nguồn và trong script vận hành; dọn cache bằng xoá theo tiền tố có chủ đích.

---

## L-22 — Rate limit của AD-3 không định nghĩa chiều khoá · MEDIUM

AD-3 bắt "rate limit tính cả trường hợp mã hãng không tồn tại" nhưng không nói **khoá theo chiều nào**.

**Cặp:** đội Gateway giới hạn theo IP; đội Auth giới hạn theo `(tenantCode, username)`. Với **một** bản triển khai phục vụ 18 hãng (AD-2), giới hạn theo IP ở gateway có thể bị một văn phòng NAT của một hãng làm đầy bucket và **khoá cửa 17 hãng còn lại**. Ngược lại, chỉ khoá theo `(tenantCode, username)` thì dò mã hãng bằng cách thử nhiều mã lại không bị chặn — phá đúng mục tiêu bảo mật của AD-3.

**AD siết lại:** rate limit là **nhiều tầng khoá đồng thời**, khai một chỗ: `(ip)` ngưỡng rộng · `(ip, tenantCode)` · `(tenantCode, username)` · và một bộ đếm riêng cho **mã hãng không tồn tại** theo `(ip)` với ngưỡng chặt hơn nhiều. Hiện thực ở **đúng một** tầng (gateway), các dịch vụ phía sau không tự làm.

---

## L-23 — Cache sổ đăng ký hãng không có TTL cũng không có đường thu hồi · MEDIUM

AD-14 bắt buộc "cache cục bộ trong tiến trình + health check riêng + chạy tiếp được khi nó tạm mất" — và không nói gì về **hết hạn** hay **thu hồi**.

**Cặp:** đội A cache vĩnh viễn với fallback (đọc đúng chữ "chạy tiếp được khi nó tạm mất"); đội B cache 5 phút. Xoay mật khẩu DB của một hãng ⇒ nửa cụm dùng chuỗi cũ và hỏng, nửa kia chạy. Vô hiệu hoá một hãng ⇒ hãng đó vẫn đăng nhập được trên các node đã cache.

**AD siết lại:** mục cache có TTL (≤ 5 phút) **và** kênh thu hồi (Redis pub/sub) **và** fallback cho bản chụp cuối khi sổ đăng ký mất, kèm giới hạn: sau 30 phút không liên lạc được sổ đăng ký, node ngừng chấp nhận **đăng nhập mới** (phiên đang chạy vẫn phục vụ). Xoay chuỗi kết nối là quy trình hai giai đoạn (ghi chuỗi mới → chờ quá TTL → thu hồi chuỗi cũ), ghi thành runbook.

---

## L-24 — JSON casing không được chốt · MEDIUM

Bảng Conventions chốt namespace, tên interface, endpoint `kebab-case`, định danh, ngày/số, hình dạng lỗi — nhưng **không chốt quy ước tên thuộc tính JSON**. `System.Text.Json` mặc định camelCase trong ASP.NET Core, còn `Newtonsoft` cấu hình sẵn theo thói quen của web cũ thì PascalCase. Hai đội, hai lựa chọn serializer, hai casing trên cùng một màn hình. Đã gộp cách khắc phục vào **AD-20** (L-15): chốt `camelCase`, enum là chuỗi, tiền và id là chuỗi, và một `JsonSerializerOptions` dùng chung trong `Staxi.Platform` — cấm cấu hình cục bộ.

---

## L-25 — Tải file không nằm gọn trong "một màn hình một lời gọi BFF" · LOW

Xuất Excel là tính năng cốt lõi của admin này (`BaseController` của web cũ có sẵn hạ tầng xuất Excel). AD-12 nói React chỉ gọi web-bff. Đội A cho file đi qua BFF (BFF phải đệm hoặc chuyển tiếp luồng, và mọi ràng buộc bộ nhớ của AD-9 quay lại ở BFF); đội B để gateway định tuyến thẳng FE → `Staxi.Reporting.Api` cho đường tải file. Hai mô hình, hai cách xử lý quyền và lỗi (tải file lỗi thì ProblemDetails hiện ra sao trong một thẻ `<a download>`? — AD-8 cũng không nói).

**AD siết lại:** thêm một ngoại lệ tường minh cho AD-12: đường tải/tải lên file đi **thẳng qua gateway tới dịch vụ sở hữu**, dùng token một lần có hạn ngắn do BFF cấp, và **luôn là luồng, không đệm**. Lỗi trên đường tải file trả ProblemDetails kèm `corrId` và FE phải xử lý bằng `fetch` + Blob, không dùng `<a download>` trần.

---

## Ba điều đúng, nói cho công bằng

1. **AD-3 (tenant phân giải trước xác thực, cấm endpoint liệt kê hãng, thông báo lỗi đồng nhất)** là một quyết định chín, hiếm khi thấy được viết ra rõ như vậy.
2. **AD-11 dám ghi ra một rủi ro đã chấp nhận thay vì giả vờ không có** — đó là dấu hiệu của một spine trung thực; vấn đề chỉ nằm ở chỗ bù đắp chưa đủ (L-04).
3. **Mục 350 bảng chưa rõ hạng tenant được nêu là điều kiện tiên quyết, không phải việc dọn dẹp** — đúng, và là chỗ tài liệu này tự nhìn thấy đáy của mình rõ nhất.

---

## Điều kiện để qua cổng

Spine chưa đủ chặt để hai đội làm song song. Tôi đề nghị **không duyệt** cho tới khi:

1. Giải quyết xong tám lỗ **critical**: L-01 · L-02 · L-03 · L-04 · L-05 · L-06 · L-07 · L-08.
2. Bổ sung năm AD mới: **AD-16** (thời gian) · **AD-17** (dữ liệu toàn cục và nơi ở vật lý) · **AD-18** (sổ chủ sở hữu màn hình) · **AD-19** (sổ đăng ký quyền) · **AD-20** (`Staxi.Contracts` và hình dạng dây).
3. Thêm mục **"AD ↔ chốt chắn"** vào spine, với mọi AD có ít nhất một cơ chế phát hiện, và AD-8 đứng đầu danh sách phải vá (L-20).
4. Sửa mốc xem lại của Deferred #1 sang "trước đợt phát hành đầu tiên có DDL" (L-19).

Nhận xét cuối, ngắn: spine này được viết bởi người đã đọc kỹ web cũ và biết nó đau ở đâu — điều đó hiện rõ trong AD-3, AD-4 và AD-8. Chỗ nó hụt là **thứ mà một người không thể tự phát hiện: hai đội cùng đọc một câu và hiểu hai nghĩa**. Bốn từ chịu trách nhiệm cho phần lớn thiệt hại: **"tenant"** (L-01), **"toàn cục"** (L-05), **"phân giải lại"** (L-06), **"định hình dữ liệu"** (L-16). Định nghĩa bốn từ đó bằng **kiểu dữ liệu và chốt chắn CI**, chứ không bằng câu văn, và spine này sẽ đứng vững.
