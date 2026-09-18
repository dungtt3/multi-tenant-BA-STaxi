# Ghi chú thiết kế — `Staxi.Platform` và `Staxi.TenantLeakTests`

Trang này giữ **lý do** của những lựa chọn cài đặt trong nền tảng. Code không mang comment, nên chỗ nào
một quyết định trông kỳ lạ hoặc trông như có cách viết gọn hơn, câu trả lời nằm ở đây.

`docs/ARCHITECTURE-SPINE.md` nói **luật gì**. Trang này nói **vì sao code trông như vậy**.

---

## `TenantScope` — vì sao là class chứ không phải struct

Một `struct` luôn có giá trị `default`. Nghĩa là luôn tồn tại sẵn một "phạm vi rỗng" hợp lệ về mặt kiểu,
và mọi hàm nhận `TenantScope` đều có thể được gọi với nó mà trình biên dịch không nói gì. Đó đúng là thứ
`AD-2` cấm: *không có đường mặc định*.

Với class, không có phạm vi nghĩa là `null`, và mọi nơi tiêu thụ phải xử lý tường minh.

**Hai đường dựng, không có đường thứ ba:**

| Hàm | Dùng khi |
|---|---|
| `FromSignedToken` | Request người dùng. Đọc từ claim của principal **đã xác thực chữ ký**. |
| `FromBackgroundJob(…, lyDo)` | Công việc chạy nền. Bắt buộc khai lý do — để grep ra và để ghi vết (`AD-15`, `AD-18`). |

`companyId` bắt buộc `> 0`. Token tạm thời (đã xác thực nhưng chưa chọn công ty) **không** dựng ra một
`TenantScope` — nó là trạng thái "chưa có phạm vi", và mọi thứ chạm dữ liệu sẽ ném. Xem luồng đăng nhập ở `AD-3`.

Mã hãng chuẩn hoá về chữ thường và kiểm theo khuôn `^[a-z0-9][a-z0-9_-]{1,31}$`. Khuôn này **cấm dấu hai chấm**
vì `:` là ký tự phân đoạn của `CacheKey` (`AD-6`) và của tên group SignalR (`AD-7`) — một mã hãng chứa `:`
sẽ giả được khoá và group của hãng khác.

---

## `ITenantScopeAccessor` tách khỏi `ITenantScopeBinder`

Hai interface cho một cài đặt, có chủ ý (ISP):

- Code nghiệp vụ chỉ tiêm interface **đọc**.
- Chỉ ba nơi được tiêm interface **gán**: adapter xác thực HTTP (`AD-4`), handshake SignalR (`AD-7`),
  và bộ khởi chạy công việc nền (`AD-15`).

`Bind` ném `TenantScopeConflictException` khi có ai đó cố gán một phạm vi **khác** đè lên phạm vi đang chạy.
Đổi công ty phải đi qua `select-company` và lấy token mới, không phải gán lại giữa chừng. Gán lại **đúng**
phạm vi đang có thì vô hại, nên được phép.

`Detach()` tồn tại vì `AsyncLocal` chảy theo mọi `Task` sinh ra từ request. Tiện, và đúng là cái bẫy: một
việc nền khởi động từ request của hãng A sẽ lặng lẽ chạy tiếp dưới phạm vi hãng A rất lâu sau khi request
đó kết thúc. Ranh giới việc nền phải `Detach()` rồi tự `FromBackgroundJob(...)`.

Dùng `AsyncLocal` thay cho `IHttpContextAccessor` để tầng Application và Infrastructure không phải biết HTTP,
và để SignalR hub method — mỗi lần gọi là một DI scope mới — dùng chung được một đường.

---

## `ITenantConnectionFactory` — chữ ký không có tham số tenant

Đó không phải sơ suất. Nếu có tham số thì sẽ có nơi truyền vào một giá trị lấy từ dữ liệu client gửi, và
đó đúng là lỗ hổng `HomeController.ChangeCompany` của web cũ (`AD-4`).

Bốn bước trong `OpenAsync`, không bỏ qua được bước nào:

1. **Phạm vi** — `Required(...)`, không có thì ném.
2. **Sổ đăng ký** — `ITenantRegistry` tra mã hãng ra connection string (`AD-14`).
3. **Mở kết nối** — qua `IDbConnectionSource`.
4. **Kiểm tại kết nối vật lý** — `IConnectionScopeVerifier` (`AD-18`).

**Vì sao tách `IDbConnectionSource`:** để chỗ gọi `new SqlConnection(...)` nằm gọn trong đúng một kiểu
(`SqlServerConnectionSource`). Bài quét assembly của `AD-15` chỉ cần cho phép đúng kiểu đó — thêm một chỗ
khác là build đỏ. Phần thưởng kèm theo: bộ test rò rỉ thay bằng SQLite mà không phải sửa một dòng nào của
`TenantConnectionFactory` (LSP + DIP).

**Vì sao bước 4 tách thành interface riêng:** `AD-18` bắt đo tại kết nối vật lý (`DB_NAME()`,
`ORIGINAL_LOGIN()`) chứ **không** lấy từ `TenantScope` của ứng dụng. Nếu vết lấy tenant từ cùng nguồn có thể
đang sai thì nó không phát hiện được chính lớp lỗi nó tồn tại để bù. Lệch hai giá trị ⇒ ném
`TenantConnectionMismatchException` và dừng request, không chỉ ghi log.

`ORIGINAL_LOGIN()` hiện mới chỉ đọc chứ chưa dùng để chặn: `AD-19` (mỗi hãng một login SQL riêng) vẫn chờ
DBA xác nhận. Khi có 18 login, chỗ này thêm được một lớp chặn nữa gần như miễn phí.

**Ghi chú `Encrypt`:** `Microsoft.Data.SqlClient` mặc định `Encrypt=true` từ bản 4.0. 18 SQL Server chạy trên
IP nội bộ ⇒ phải cấp chứng chỉ tin cậy. Tuyệt đối không "chữa" bằng `TrustServerCertificate=true` — cái đó
vô hiệu hoá chính lớp mã hoá đường truyền.

---

## `CachingTenantRegistry` — hai yêu cầu đối nghịch

`AD-14` đòi cả hai thứ ngược nhau, và cả hai đều bắt buộc:

- DB quản trị là **điểm chết đơn lẻ** của 18 hãng ⇒ khi nó tạm mất, phải phục vụ bản cache **cũ** để hệ thống chạy tiếp.
- Công tắc vô hiệu một hãng phải **cắt cả phiên đang chạy** ⇒ TTL phải ngắn, và hãng đã tắt thì **không** được phục vụ từ bản cũ.

Cách hoà giải: TTL ngắn; hết hạn thì nạp lại; nạp lại lỗi thì dùng bản cũ **chỉ khi** bản cũ đang bật, và
log cảnh báo mỗi lần. Hãng đã tắt bị xoá khỏi cache ngay lập tức.

Mặc định TTL một phút. Bộ test ép xuống 200 ms để bài kiểm công tắc vô hiệu hoá chạy trong vài trăm mili giây
thay vì một phút.

---

## `CacheKey` — không có constructor thiếu phạm vi

Cùng lý do với `TenantScope`: là class chứ không phải struct, vì struct luôn có `default` — tức luôn tồn tại
sẵn một khoá rỗng hợp lệ, đúng thứ `AD-6` cấm.

Khuôn `{tenantCode}:{companyId}:{vùng}:{khoá}`, thêm `#p{permissionVersion}` khi nội dung phụ thuộc quyền.
Thu hồi quyền mà khoá không đổi thì cache tiếp tục phục vụ nội dung của quyền cũ (`AD-17`).

Vùng và khoá **cấm** chứa `:`, `#`, khoảng trắng. Không phải để cho đẹp: một khoá bịa khéo sẽ giả được khoá
của hãng khác.

**`CacheKey.Shared` nhận `SharedReferenceItem` chứ không nhận `string`:** `SharedReferenceItem` không có
constructor công khai, cách duy nhất có một thể hiện là lấy từ `SharedReferenceData.Catalog`. Nhờ vậy "key
dùng chung" không dựng ra được từ một chuỗi tự nghĩ — nó phải là một mục đã khai trong mã nguồn và đã qua review.

**Sổ `SharedReferenceData` hiện RỖNG, và đó là câu trả lời đúng cho gần như mọi thứ.** Mỗi hãng có database
riêng nên danh mục là của riêng hãng đó: tỉnh thành, loại xe, mã lỗi ở hãng A có id khác hãng B. Đưa một danh
mục vào sổ vì "nó giống nhau mà" chính là cách danh mục của hãng A được phục vụ cho cả 18 hãng.

Điều kiện để thêm một dòng: (1) dữ liệu **không** đến từ database của bất kỳ hãng nào; (2) id ổn định toàn hệ
thống; (3) có người duyệt. Ví dụ hợp lệ trong tương lai: bảng mã tiền tệ ISO 4217 nạp từ file trong mã nguồn.

---

## `TenantScopedRepository` — mỗi hạng tenant một hàm

Không có hàm "truy vấn tự do". Muốn chạy SQL không phạm vi thì phải gọi tên nó ra.

| Hàm | Ép lúc chạy |
|---|---|
| `QueryPerCompanyAsync` | SQL **bắt buộc** có `@CompanyId`; thiếu ⇒ `ThieuBoLocPhamViException` |
| `QueryPerTenantAsync` | SQL **bắt buộc không** có `@CompanyId`; database đã là biên ⇒ có là `SaiHangTenantException` |
| `AcrossTenants(lyDo, …)` | Bắt khai lý do, ghi vết **trước khi chạy**, log cảnh báo |

**Vì sao ép bằng "SQL phải chứa `@CompanyId`" chứ không tự ghép thêm `AND CompanyId = …`:** ghép chuỗi SQL
là thứ vỡ âm thầm khi gặp `UNION`, subquery, `GROUP BY`. Bắt người viết đặt tham số vào đúng chỗ họ hiểu
thì nó hoặc đúng, hoặc đỏ ngay — không có vùng xám.

**Giá trị `CompanyId` được ghi đè sau cùng**, kể cả khi người gọi truyền vào giá trị của riêng họ. Phạm vi
thắng, luôn luôn (`AD-4`).

`AcrossTenants` vượt **công ty** trong một hãng. Nó không vượt **hãng** — vượt hãng không có đường nào cả,
vì mỗi hãng một database.

`LoggingAcrossTenantAuditSink` là **chỗ giữ chỗ có ý thức**, không phải cài đặt cuối. `AD-18` đòi kho vết
chỉ-ghi-thêm, tách khỏi database nghiệp vụ; cái đó chưa dựng.

---

## `PlatformProblemDetailsMiddleware`

Sai lầm lớn nhất của web cũ là HTTP 200 kèm trang lỗi HTML: không giám sát nào đếm được lỗi, client không
phân biệt được thành công với thất bại.

- `corrId` gắn vào **mọi** phản hồi, kể cả thành công. Lấy theo `traceparent` của W3C khi có (`AD-13`).
- `WriteAsJsonAsync` **phải** truyền `contentType` tường minh — mặc định nó ghi đè thành `application/json`,
  trong khi RFC 9457 quy định `application/problem+json`. Bộ test rò rỉ đã bắt được đúng lỗi này.
- Thông điệp ngoại lệ **không ra ngoài**: nó có thể chứa mã hãng, tên database, câu SQL. Người dùng nhận
  `corrId`, người vận hành tra log bằng `corrId`.
- Khi phản hồi đã bắt đầu gửi thì không sửa được mã trạng thái nữa — đó là tình huống **duy nhất** có thể ra
  một phản hồi 200 kèm lỗi, nên nó là log mức `Critical` chứ không im lặng.

Bảng phân loại ngoại lệ → mã trạng thái nằm trong `PhanLoai`. Đáng chú ý: `TenantKhongTonTaiException` trả
**403 với thông điệp chung**, không tiết lộ là hãng không tồn tại hay hãng đã bị tắt (`AD-3`).

---

## `ScopeParameterGuardMiddleware` — và giới hạn đã biết của nó

Chặn tham số tự chọn phạm vi trong query, route, form, header. **Chặn chứ không bỏ qua im lặng:** bỏ qua thì
client vẫn tưởng mình đổi được hãng, và lớp phòng thủ này không để lại số liệu nào để đếm.

Header `X-Tenant*` / `X-Company*` đã bị gateway xoá (`AD-3`); còn tới được đây nghĩa là ai đó đi vòng qua gateway.

**Giới hạn:** middleware này so khớp theo **tên**. Lệnh cấm của `AD-4` theo **khái niệm** — "bất kể đặt tên là
gì". Phần quét DTO và chữ ký tham số thuộc `Staxi.ArchitectureTests` (`AD-15`), **chưa dựng**. Lưới chặn tên
là lớp thứ nhất, không phải lớp duy nhất: lớp thật nằm ở chỗ repository lấy `CompanyId` từ phạm vi.

---

## SignalR — `TenantHub`, `TenantGroups`, `ITenantNotifier`

Đây là lớp rò rỉ khó thấy nhất: một bản tin phát nhầm khách **không** để lại log, **không** ai báo, và không
có mã trạng thái HTTP nào sai cả.

- Phạm vi lấy từ token lúc handshake, ghi vào `Context.Items`, **phân giải lại từ đó ở mỗi lần gọi** — vì mỗi
  lần gọi hub method là một DI scope mới, phạm vi gán lúc handshake không còn nằm trong `AsyncLocal` của lần gọi đó.
- Kết nối không có phạm vi hợp lệ trong token thì **bị đóng**. Không có kết nối "chưa phân hạng" nằm chờ, vì
  một kết nối như thế sẽ phải lấy phạm vi từ đâu đó khác — tức từ client.
- Tên group sinh bởi **một hàm duy nhất** (`TenantGroups.For`). Hai chỗ tự ghép chuỗi là hai cách đặt tên.
- `IHubContext` bị bọc sau `ITenantNotifier`, và chữ ký bắt buộc có `TenantScope` nên không phát được cho
  "tất cả". `Clients.All` / `Others` / `AllExcept` bị cấm.

---

## Bộ test rò rỉ — vài lựa chọn cần giải thích

**Vì sao SQLite chứ không phải SQL Server:** bộ test của `AD-24` phải chạy trong CI mọi lần, không được phụ
thuộc một máy chủ nào. Thứ cần kiểm là **đường đi của phạm vi** — factory chọn database nào — chứ không phải
phương ngữ SQL. Mỗi hãng vẫn là một file database riêng, đúng hình dạng của 18 DB thật.

**Vì sao dữ liệu hãng B mang chữ `HANG-B`:** để một lần rò rỉ là nhìn thấy ngay, không phải chuyện phải suy luận.

**Vì sao `LoaiXe` dùng thuộc tính có setter chứ không phải record positional:** Dapper khớp constructor theo
kiểu **chính xác**, mà SQLite trả số nguyên về `Int64`.

**Vì sao bộ test chạy tuần tự** (`CollectionBehavior(DisableTestParallelization = true)` trong `AssemblyInfo.cs`):
phạm vi truyền bằng `AsyncLocal` tĩnh. Trong một tiến trình production đó là đúng một container và đúng một
luồng request. Nhưng xUnit chạy nhiều lớp test song song trong **cùng** tiến trình, với nhiều `TestServer`
cùng lúc — tức nhiều "ứng dụng" dùng chung một ô `AsyncLocal`. Một bộ test có nhiệm vụ chứng minh không có rò
rỉ mà thỉnh thoảng đỏ vì chính nó nhiễu lẫn nhau thì tệ hơn là không có: lần đỏ thật sẽ bị cho là "lại flaky".
Sáu giây chạy tuần tự là cái giá rẻ.

**Vì sao đăng ký SQLite TRƯỚC `AddStaxiPlatform()`:** nền tảng dùng `TryAdd`, nên bản đăng ký đầu tiên thắng.
Mọi thứ còn lại trong bộ test là mã production.

---

## Hai thứ trong `Directory.Build.props` / `.editorconfig` có lý do

- `ValidateScopes` và `ValidateOnBuild` bật ở **mọi** môi trường, kể cả production (`AD-2`). Mặc định của .NET
  chỉ bật ở Development. Nhưng đúng thứ hai cơ chế này bắt được — một singleton lỡ giữ một dịch vụ scoped —
  lại là cách `TenantScope` của hãng A sống sót sang request của hãng B. Giá phải trả là vài mili giây lúc khởi động.
- `CA1848` (LoggerMessage delegate) hạ xuống `suggestion`. Nền tảng có vài chỗ ghi log và đều ở đường người
  hoặc đường lỗi; chưa đo được cái giá thực. Xem lại khi có số đo hiệu năng.
- `CA1707` tắt trong `tests/` — tên bài test dùng gạch dưới để đọc được dạng `TinhHuong_KetQua`.

---

## `IAuthenticationConnectionFactory` — vì sao phải có một đường thứ hai

`ITenantConnectionFactory` đòi phải có `TenantScope`. Nhưng **xác thực diễn ra trước khi phạm vi tồn tại**:
lúc người dùng mới gõ mã hãng và mật khẩu, chưa có gì để dựng ra một `TenantScope` hợp lệ — `companyId`
còn chưa biết.

Nếu không có đường riêng, người viết `Staxi.Auth` chỉ còn hai lựa chọn, và cả hai đều tệ:

- Phá `AD-2` bằng cách tự `new SqlConnection` — mất luôn phần kiểm tại kết nối vật lý của `AD-18`.
- Bịa một `TenantScope` giả với `companyId` bất kỳ — tức nói dối chính cái kiểu mà cả hệ thống dựa vào.

Nên đường thứ hai là **có chủ ý**, và được siết bằng ba thứ:

1. **Tên gọi tự tố cáo.** `OpenForAuthenticationAsync` không phải thứ ai đó gọi nhầm rồi bảo không biết.
2. **Bắt khai lý do**, giống `AcrossTenants(lyDo)` — grep ra được, và ghi log ở mức `Information` chứ không `Debug`.
3. **Luật `R16` trong `Staxi.ArchitectureTests`:** chỉ assembly `Staxi.Auth.*` được gọi. Gọi từ chỗ khác là build đỏ.

Nó **vẫn** đi qua sổ đăng ký hãng và **vẫn** kiểm tại kết nối vật lý — chỉ bỏ đúng một thứ là `TenantScope`,
vì thứ đó chưa thể tồn tại. Nhãn phạm vi truyền cho bộ kiểm là `xác thực:{tenantCode}`, nên nếu kết nối mở
nhầm database thì lỗi vẫn nói rõ chuyện gì xảy ra.

Đó cũng là lý do `IConnectionScopeVerifier` nhận **một nhãn chuỗi** thay vì nhận `TenantScope`: nó chỉ cần
biết gọi tên chỗ sai trong thông điệp lỗi, không cần biết phạm vi.
