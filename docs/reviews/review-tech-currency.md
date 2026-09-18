# Review — Độ tươi công nghệ & đối chiếu thực tế

- **Tài liệu soát:** `ARCHITECTURE-SPINE.md` (WEB2 — nền tảng admin multi-tenant BA STaxi)
- **Lăng kính:** technology currency / reality-check
- **Ngày soát:** 2026-09-17
- **Cách soát:** đối chiếu trực tiếp với NuGet flat-container API, npm registry API, GitHub Releases API và tài liệu Microsoft Learn. Không kết luận nào dưới đây lấy từ trí nhớ mô hình.

---

## Phán quyết

**Phần số phiên bản: đúng. Phần tương thích: chưa được kiểm.**

Cả 12 phiên bản có pin trong bảng Stack đều **thật sự là bản ổn định mới nhất** tại 2026-09-17 — tôi đã đối chiếu từng cái một với registry. Câu *"Đã kiểm chứng trên NuGet và npm ngày 2026-09-17"* là thành thật ở mức con số.

Nhưng độ tươi không phải là thứ duy nhất cần kiểm, và đây là chỗ tài liệu hụt: **các thư viện được chọn riêng lẻ, không được kiểm khi ghép lại với nhau.** Ba lỗ hổng lớn nhất đều nằm ở khoảng nối:

1. TypeScript 7.0.2 là bản mới nhất — nhưng chưa có công cụ lint nào chạy được với nó.
2. StackExchange.Redis 3.2.1 là bản mới nhất — nhưng backplane SignalR mà AD-7 bắt buộc lại biên dịch trên 2.7.27.
3. Backplane SignalR và client FE — hai thứ AD-7 sống chết dựa vào — **không hề có trong bảng Stack**; thứ được pin (`SignalR.Client`) lại là gói sai.

Bảng Stack cũng bỏ trống mọi thành phần hạ tầng đang gánh phần lớn rủi ro: SQL Server (không có mức vá tối thiểu), Node.js, Redis server, HAProxy, OpenTelemetry.

**Phải sửa trước khi có ai dựng code từ tài liệu này:** 3 mục HIGH.

---

## Bảng tổng hợp

| # | Mức | Hạng mục | Kết luận |
|---|---|---|---|
| 1 | **HIGH** | TypeScript 7.0.2 | Không công cụ lint nào hỗ trợ |
| 2 | **HIGH** | SignalR backplane thiếu + xung đột SE.Redis | Gói AD-7 cần không có trong Stack |
| 3 | **HIGH** | StackExchange.Redis 3.2.1 | Major mới 7 tuần, patch mới 4 ngày |
| 4 | MEDIUM | grate | Không pin; quy ước thư mục nghi sai |
| 5 | MEDIUM | SQL Server | Không có mức vá tối thiểu; CVE-2026-21262 |
| 6 | MEDIUM | Microsoft.Data.SqlClient | Claim ĐÚNG; thiếu bẫy `Encrypt=true` |
| 7 | MEDIUM | Node.js | Không pin, trong khi Vite 8 / ESLint 10 có sàn |
| 8 | MEDIUM | OpenTelemetry | AD-13 bắt buộc nhưng không pin gì |
| 9 | MEDIUM | Yarp.ReverseProxy 2.3.0 | Là mới nhất, nhưng footnote TFM sai |
| 10 | LOW | react-router-dom | Là gói vỏ; nên pin `react-router` |
| 11 | LOW | Hạ tầng còn lại | HAProxy / Redis / Jenkins / IIS không pin |
| 12 | INFO | .NET 10 / .NET 8 EOL | Xác nhận đúng |
| 13 | INFO | Các pin còn lại | Xác nhận đúng toàn bộ |

---

## Chi tiết phát hiện

### 1. HIGH — TypeScript 7.0.2: đúng là mới nhất, nhưng chưa có bộ lint nào chạy được

**Tài liệu nói:** bảng Stack ghi `TypeScript | 7.0.2`, không kèm ghi chú nào.

**Tôi tìm thấy:**

- `typescript` dist-tag `latest` = **7.0.2**, phát hành **2026-07-08**. Đúng.
- Nhưng `typescript-eslint` bản mới nhất là **8.70.0** (2026-09-07) và peer dependency của nó là:

  ```
  "typescript": ">=4.8.4 <6.1.0"
  ```

  → **TypeScript 7.0.2 nằm ngoài dải hỗ trợ.** Cài chung sẽ báo xung đột peer, và nếu ép `--legacy-peer-deps` thì parser đọc AST bằng API cũ, hành vi không bảo đảm.
- Nguyên nhân gốc: TS 7.0 là bản viết lại bằng Go, **chưa có programmatic API ổn định** — theo thông báo phát hành, API này dự kiến tới **7.1** mới có. Mọi công cụ dựa vào compiler API (typescript-eslint, nhiều codegen, một số plugin test) đều chưa lên được.
- Lối thoát "dùng TS 6.0 trước rồi nhảy" mà các bài viết khuyên **không dùng được ở đây**: trên npm, `6.0.0` **chưa từng phát hành bản ổn định** — dist-tag `beta` vẫn đang trỏ `6.0.0-beta`. Nghĩa là dải `<6.1.0` mà typescript-eslint chấp nhận, trên thực tế chỉ còn tới **5.9.x**.

**Nguồn:**
- https://registry.npmjs.org/typescript (dist-tags + time)
- https://registry.npmjs.org/typescript-eslint (peerDependencies)
- https://devblogs.microsoft.com/typescript/announcing-typescript-7-0-beta/
- https://www.infoq.com/news/2026/08/typescript-7-released/

**Sửa cụ thể:** Đây là quyết định phải nêu tường minh, không được để trống. Chọn một trong hai và ghi vào tài liệu:

- **(a) Pin `TypeScript 5.9.2`** cho đợt đầu, kèm dòng trong mục Deferred: *"Nâng lên TypeScript 7 khi typescript-eslint hỗ trợ TS 7 (chờ programmatic API ở TS 7.1)"*. Đây là lựa chọn an toàn cho một nền tảng sẽ sống 3 năm.
- **(b) Giữ `TypeScript 7.0.2`** nhưng phải ghi rõ: đợt đầu **không có lint theo kiểu (type-aware linting)**, và nêu thứ thay thế (ví dụ dùng `oxlint` / `biome` — cần tự kiểm tra hai thứ này trước khi cam kết).

Dù chọn gì, thêm một dòng vào bảng **Deferred** với điều kiện xem lại là *"typescript-eslint công bố hỗ trợ TS 7"*.

---

### 2. HIGH — AD-7 bắt buộc backplane Redis, nhưng gói backplane không có trong Stack, và gói được pin là gói sai

**Tài liệu nói:**
- AD-7: *"Backplane Redis có tiền tố `{môi-trường}:{hãng}`"* — đây là một invariant, không phải gợi ý.
- Bảng Stack: `Microsoft.AspNetCore.SignalR.Client | 10.0.12`.

**Tôi tìm thấy ba vấn đề chồng lên nhau:**

**(a) Gói được pin là gói sai.** `Microsoft.AspNetCore.SignalR.Client` là client **.NET** — dùng khi một tiến trình .NET kết nối tới hub. Ở WEB2, client là **React chạy trong trình duyệt**, nên thứ thật sự cần là gói npm `@microsoft/signalr`. Gói này **không có trong bảng Stack**.

- `@microsoft/signalr` dist-tag `latest` = **10.0.11** (2026-08-04).
- Lưu ý: bản npm (10.0.11) **đi sau** bản .NET (10.0.12). Đây là chuyện bình thường nhưng nên biết để không đi tìm 10.0.12 trên npm.

**(b) Phía server không có gói nào được pin.** Hub SignalR nằm sẵn trong ASP.NET Core nên không cần gói riêng — **nhưng backplane thì cần**: `Microsoft.AspNetCore.SignalR.StackExchangeRedis`. Không có trong bảng Stack, dù AD-7 bắt buộc.

**(c) Và đây là phần nghiêm trọng nhất — xung đột phiên bản.** Tôi đọc nuspec của `Microsoft.AspNetCore.SignalR.StackExchangeRedis 10.0.12`:

```
<group targetFramework="net10.0">
  <dependency id="MessagePack" version="2.5.302" />
  <dependency id="Microsoft.Extensions.Options" version="10.0.12" />
  <dependency id="StackExchange.Redis" version="2.7.27" />
</group>
```

Backplane chính thức của Microsoft cho .NET 10 **vẫn biên dịch trên StackExchange.Redis 2.7.27**. Tài liệu lại pin **3.2.1** — một major khác. NuGet sẽ hợp nhất lên 3.2.1 (bản cao thắng), nên build vẫn xanh; nhưng backplane sẽ chạy trên một assembly mà nó **chưa từng được kiểm thử cùng**, và 3.x là một bản viết lại phần lõi (xem mục 3).

Rủi ro không trừu tượng: nếu backplane lỗi lúc chạy, triệu chứng là **bản tin không tới được instance khác** — đúng loại lỗi mà AD-7 mô tả là *"lỗi không để lại log và không ai báo"*.

**Nguồn:**
- https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.signalr.stackexchangeredis/10.0.12/microsoft.aspnetcore.signalr.stackexchangeredis.nuspec
- https://registry.npmjs.org/@microsoft/signalr

**Sửa cụ thể:** Thay dòng SignalR trong bảng Stack bằng ba dòng:

| Name | Version | Ghi chú |
|---|---|---|
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` | 10.0.12 | backplane (server) |
| `@microsoft/signalr` (npm) | 10.0.11 | client React |
| ~~`Microsoft.AspNetCore.SignalR.Client`~~ | — | bỏ, trừ khi thật sự có client .NET |

Và **phải quyết** chuyện SE.Redis: hoặc hạ xuống dòng 2.8.x mà backplane đã kiểm thử, hoặc giữ 3.2.1 và bổ sung **một bài kiểm tích hợp hai instance** chứng minh bản tin đi qua backplane đúng group — việc này nên gắn thẳng vào AD-15 (CI).

---

### 3. HIGH — StackExchange.Redis 3.2.1 là một major mới toanh, pin ở bản patch 4 ngày tuổi

**Tài liệu nói:** `StackExchange.Redis | 3.2.1`, không ghi chú.

**Tôi tìm thấy:** 3.2.1 đúng là bản ổn định mới nhất, và **có TFM `net10.0` thật** (điểm cộng). Nhưng lịch sử phát hành cho thấy đây là vùng đất rất mới:

| Bản | Ngày |
|---|---|
| 3.0.25 | 2026-07-29 |
| 3.1.0 | 2026-07-31 |
| 3.1.31 | 2026-08-21 |
| 3.2.0 | 2026-09-10 |
| **3.2.1** | **2026-09-13** |

Toàn bộ dòng 3.x mới khoảng **7 tuần tuổi**; bản được pin mới **4 ngày** tính tới ngày lập tài liệu. Trong 7 tuần đó đã có **8 bản phát hành** — nhịp vá của một thư viện chưa ổn định.

Ngoài ra:
- 3.x thay phần lõi bằng **RESPite** (nuspec 3.2.1 phụ thuộc `RESPite 3.2.1`) — không phải nâng cấp thường.
- **3.2.0 có breaking change:** bỏ TFM `net6.0`; người dùng .NET 6 rơi về netstandard và **mất hỗ trợ unix domain socket**.
- 3.1.0 biến các cảnh báo deprecation thành **lỗi biên dịch**.

**Nguồn:**
- https://api.github.com/repos/StackExchange/StackExchange.Redis/releases
- https://api.nuget.org/v3-flatcontainer/stackexchange.redis/3.2.1/stackexchange.redis.nuspec

**Sửa cụ thể:** Redis là thành phần nền của cả AD-6 (cache key) lẫn AD-7 (backplane) — hai invariant. Ghi vào tài liệu một trong hai:

- Pin dòng **2.8.x** (dòng mà backplane SignalR đã kiểm thử) cho đợt đầu, và đưa "nâng SE.Redis lên 3.x" vào **Deferred** với điều kiện *"khi backplane SignalR chính thức phụ thuộc 3.x"*. Đây là lựa chọn tôi khuyên.
- Hoặc giữ 3.2.1, nhưng phải bổ sung: (i) **pin phiên bản Redis server tối thiểu** vào bảng Stack — 3.x mặc định thương lượng RESP3, hành vi khác RESP2 và phụ thuộc phiên bản server; (ii) bài kiểm tích hợp ở mục 2.

---

### 4. MEDIUM — grate: không pin phiên bản, và quy ước thư mục trong AD-10 có vẻ sai

**Tài liệu nói:**
- Bảng Stack: `grate | công cụ chạy migration cho 18 DB` — **ô Version bỏ trống**.
- AD-10: *"DDL ... chạy bằng grate — sprocs/views/functions đặt ở **thư mục `AnyTime`** để chạy lại khi nội dung đổi."*
- Cây nguồn: `db/migrations/  # grate: Once / AnyTime / EveryTime`

**Tôi tìm thấy:**

- **Bản mới nhất: `grate 2.1.6`**, phát hành **2026-07-29**. (Một tìm kiếm web trả về "2.1.5, 07/07/2026" — đã lạc hậu; NuGet flat-container và GitHub Releases đều xác nhận 2.1.6.)
- **Còn được bảo trì:** có, phát hành đều; notes 2.1.6 là bump phụ thuộc + cải tiến Docker buildx.
- **Hỗ trợ SQL Server:** **có** — SQL Server, PostgreSQL, MariaDB/MySQL, SQLite, Oracle.
- **Hỗ trợ .NET 10:** **có** — mô tả gói ghi nguyên văn *"written from the ground up using modern .NET 8/9/10"*.
- **Nhưng:** nuspec khai `<packageType name="DotnetTool" />`. Đây là **dotnet global/local tool**, nên **máy chạy Jenkins phải có .NET runtime tương ứng**. Đây là một điều kiện triển khai mà tài liệu chưa nêu. (Trang chủ grate cũng nhắc có bản **self-contained executable** — nếu không muốn cài runtime lên agent thì đó là đường thay thế; cần tự chọn và ghi rõ.)
- **Nghi vấn về quy ước thư mục:** "Once / AnyTime / EveryTime" là tên **loại script** trong grate, không phải tên thư mục trên đĩa. Tài liệu grate liệt kê các thư mục theo tên riêng (`up`, `views`, `sprocs`, `functions`, `runAfterOtherAnyTimeScripts`, ...). Tôi **không xác minh được tên thư mục chính xác** từ trang chủ và không muốn khẳng định thay — nhưng câu *"đặt ở thư mục `AnyTime`"* trong AD-10 gần như chắc chắn **không phải là đường dẫn chạy được**.

**Nguồn:**
- https://api.nuget.org/v3-flatcontainer/grate/index.json
- https://api.nuget.org/v3-flatcontainer/grate/2.1.6/grate.nuspec
- https://github.com/grate-devs/grate/releases
- https://grate-devs.github.io/grate/

**Sửa cụ thể:**
1. Điền bảng Stack: `grate | 2.1.6 (dotnet tool)`.
2. Thêm vào AD-10 hoặc mục triển khai: *"grate chạy dạng dotnet tool — agent Jenkins cần .NET runtime tương ứng; hoặc dùng bản self-contained executable."*
3. **Mở tài liệu grate mục "Script types" và chép đúng tên thư mục** vào AD-10 và vào cây nguồn, thay cho `Once / AnyTime / EveryTime`. Đây là chỗ người ta sẽ copy nguyên văn khi dựng repo — sai tên thư mục nghĩa là script không chạy, và grate **không báo lỗi khi một thư mục không tồn tại**, nó chỉ bỏ qua.

---

### 5. MEDIUM — SQL Server: "giữ nguyên bản đang chạy" không phải một quyết định, và có CVE leo thang lên sysadmin

**Tài liệu nói:** `SQL Server | giữ nguyên bản đang chạy`.

**Tôi tìm thấy:** **CVE-2026-21262** — lỗ hổng elevation of privilege trong SQL Server, do kiểm tra quyền nội bộ không ép đúng ranh giới role ở một số thao tác. Kẻ tấn công **đã xác thực** có thể **leo lên sysadmin qua mạng**. Ảnh hưởng **mọi phiên bản từ SQL Server 2016 SP3 tới 2025**; Microsoft vá trong Patch Tuesday **tháng 3/2026**, qua CU hoặc GDR tuỳ phiên bản.

Vì sao điều này đặc biệt quan trọng với chính tài liệu này: toàn bộ mô hình cách ly tenant của WEB2 **đặt trên ranh giới database** — AD-2 ("mỗi hãng giữ database riêng"), AD-5, AD-14. Một tài khoản leo được lên sysadmin thì **đọc được cả 18 DB khách**, và đọc luôn DB quản trị chứa connection string. Kiến trúc có chặt tới đâu ở tầng ứng dụng cũng không bù được.

Đồng thời, câu "giữ nguyên bản đang chạy" nghĩa là **không ai biết bản đang chạy là gì** — trong khi cùng tài liệu này lại thống kê rất chi tiết 565 bảng / 2.003 proc. Đã đếm được proc thì đọc được `SELECT @@VERSION`.

**Nguồn:**
- https://socprime.com/blog/cve-2026-21262-vulnerability/
- https://www.rapid7.com/blog/post/em-patch-tuesday-march-2026/
- https://learn.microsoft.com/en-us/sql/connect/ado-net/download-microsoft-sqlclient-data-provider

**Sửa cụ thể:** Thay ô Version bằng một con số thật: chạy `SELECT @@VERSION, SERVERPROPERTY('ProductUpdateLevel')` trên từng instance của 18 hãng, rồi ghi **phiên bản + mức CU tối thiểu** vào bảng Stack. Thêm một dòng vào bảng **Rủi ro đã chấp nhận** hoặc **Deferred**: *"Xác minh toàn bộ instance đã vá CVE-2026-21262 (bản vá 3/2026) trước khi hãng đầu tiên lên WEB2."*

---

### 6. MEDIUM — Microsoft.Data.SqlClient: claim về TFM **đúng**, nhưng thiếu bẫy `Encrypt=true`

**Tài liệu nói:** `Microsoft.Data.SqlClient | 7.0.3`, kèm footnote *"chưa có TFM net10.0 riêng; asset net8.0/net9.0 chạy bình thường trên .NET 10."*

**Tôi tìm thấy — phần claim là đúng, xác nhận trực tiếp trên nuspec:**

- `7.0.3` đúng là bản ổn định mới nhất (dòng preview mới nhất là `7.1.0-preview3.26238.4`).
- TFM của 7.0.3: `.NETFramework4.6.2`, `net8.0`, `net9.0`, `.NETStandard2.0` → **đúng là không có `net10.0`**.
- Tôi kiểm thêm cả preview: `7.1.0-preview3` **cũng chưa có `net10.0`**. Nên câu trả lời cho *"đã có bản nhắm net10.0 chưa"* là **chưa, kể cả ở preview**.
- Và có bằng chứng chính thức củng cố kết luận "chạy bình thường trên .NET 10": Microsoft công bố SqlClient 7.0 *"compiles and tests against the .NET 10 SDK"*. Nên đây không còn là suy đoán.

**Nhưng tài liệu thiếu một bẫy di trú rất cụ thể:** từ dòng 4.0 trở đi, Microsoft.Data.SqlClient **mặc định `Encrypt=true`** (khác hẳn `System.Data.SqlClient` cũ mà `BA.STaxi.Web` đang dùng). Với 18 SQL Server nội bộ dùng chứng chỉ tự ký, connection string bê nguyên từ hệ cũ sang sẽ **fail ngay lúc mở kết nối**.

Chỗ này va thẳng vào AD-14: connection string được **mã hoá trong một cột** của sổ đăng ký. Nghĩa là nếu quên `Encrypt` / `TrustServerCertificate`, phải **sửa dữ liệu đã mã hoá cho từng hãng** — đắt hơn nhiều so với sửa một file config.

**Nguồn:**
- https://api.nuget.org/v3-flatcontainer/microsoft.data.sqlclient/7.0.3/microsoft.data.sqlclient.nuspec
- https://api.nuget.org/v3-flatcontainer/microsoft.data.sqlclient/7.1.0-preview3.26238.4/microsoft.data.sqlclient.nuspec
- https://techcommunity.microsoft.com/blog/sqlserver/microsoft-data-sqlclient-7-0-is-here-a-leaner-more-modular-driver-for-sql-server/4503173

**Sửa cụ thể:**
1. Giữ nguyên pin 7.0.3 và footnote — **đã kiểm, đúng**. Bổ sung nguồn Microsoft vào footnote để người sau không phải kiểm lại.
2. Thêm một quy tắc vào AD-14: *"Mỗi dòng trong sổ đăng ký phải khai tường minh `Encrypt` và `TrustServerCertificate`; SqlClient ≥ 4.0 mặc định `Encrypt=true`, khác hệ cũ."*

---

### 7. MEDIUM — Node.js không được pin, trong khi chuỗi công cụ FE có sàn phiên bản cứng

**Tài liệu nói:** AD-1 rất dứt khoát về runtime backend (*".NET 8 hết hỗ trợ 10/11/2026 nên không được dùng cho code mới"*) và về việc FE **không** cần Node phía server. Nhưng FE vẫn cần Node **để build**, và bảng Stack không có dòng nào cho Node.

**Tôi tìm thấy — sàn phiên bản đọc thẳng từ trường `engines`:**

| Gói | `engines.node` |
|---|---|
| `vite@8.3.0` | `^20.19.0 \|\| >=22.12.0` |
| `eslint@10.10.0` | `^20.19.0 \|\| ^22.13.0 \|\| >=24` |

Giao của hai điều kiện: **Node `^20.19` hoặc `>=22.13`**. Node 20 đã ở cuối vòng đời (cần tự xác nhận ngày EOL chính xác trước khi ghi vào tài liệu), nên lựa chọn thực tế là **Node 24 LTS**.

Đây không phải chuyện nhỏ về hình thức: AD-1 lấy lý do "nền tảng mới ra đời đã mang sẵn một cuộc nâng cấp" để loại .NET 8. Chính lý do đó áp dụng y hệt cho Node trên agent build — và hiện đang bị bỏ trống.

**Nguồn:**
- https://registry.npmjs.org/vite/latest
- https://registry.npmjs.org/eslint

**Sửa cụ thể:** Thêm dòng `Node.js | 24.x LTS (build-time; agent Jenkins)` vào bảng Stack, và nêu sàn tối thiểu `>=22.13` để người khác biết vì sao.

---

### 8. MEDIUM — AD-13 bắt buộc OpenTelemetry nhưng không pin gói nào

**Tài liệu nói:** AD-13 — *"Trace/metrics/logs theo OpenTelemetry, truyền ngữ cảnh bằng `traceparent` W3C"*, là invariant ràng buộc **mọi dịch vụ**. Bảng Stack **không có dòng OpenTelemetry nào**.

**Tôi tìm thấy:** `OpenTelemetry.Extensions.Hosting` bản mới nhất **1.18.0** (còn có `1.18.0-rc.1` trước đó). Dự án đang hoạt động bình thường.

Đây là mẫu lặp lại: mục Invariants gọi tên công nghệ, bảng Stack không pin. AD-13 còn ràng buộc thêm — *"chính sách che viết trước khi bật trace"* — nên không chỉ thiếu phiên bản mà còn thiếu cả lựa chọn exporter (OTLP → Collector? backend nào?).

**Nguồn:** https://api.nuget.org/v3-flatcontainer/opentelemetry.extensions.hosting/index.json

**Sửa cụ thể:** Thêm `OpenTelemetry.Extensions.Hosting | 1.18.0` và gói exporter tương ứng (thường là `OpenTelemetry.Exporter.OpenTelemetryProtocol`, **cần tự kiểm phiên bản**) vào bảng Stack. Nêu backend nhận trace, hoặc đưa việc chọn backend vào **Deferred** cho tường minh.

Lưu ý thêm: Serilog (đã pin `Serilog.AspNetCore 10.0.0`) và OpenTelemetry logging **chồng vai trò**. Tài liệu nên nói rõ ai làm gì — nếu không, mỗi dịch vụ sẽ tự chọn, đúng thứ AD-13 muốn ngăn.

---

### 9. MEDIUM — Yarp.ReverseProxy 2.3.0: pin **đúng**, nhưng footnote TFM sai và thiếu cảnh báo về tuổi

**Tài liệu nói:** `Yarp.ReverseProxy | 2.3.0`, footnote chung với SqlClient: *"chưa có TFM net10.0 riêng; asset net8.0/net9.0 chạy bình thường trên .NET 10."*

**Tôi tìm thấy:**

**(a) 2.3.0 đúng là mới nhất — đã kiểm kỹ.** Toàn bộ danh sách phiên bản trên NuGet flat-container kết thúc ở `2.3.0`; **không có 2.4, không có 3.0, không có cả bản preview nào mới hơn**. Roadmap chính thức trên `dotnet/yarp` cũng không nêu bản nào sau 2.3. Câu hỏi *"có YARP mới hơn đi kèm .NET 10 không"* → **không có**.

**(b) Hỗ trợ .NET 10 — có bằng chứng chính thức.** Microsoft Learn, trang YARP getting-started, moniker `aspnetcore-10.0`, cập nhật **2026-08-08**, ghi nguyên văn: *"YARP 2.3.0 supports .NET 8 or later"* và khuyên dùng *"version 2.3.0 or later"*. Claim của tài liệu đứng vững.

**(c) Nhưng footnote sai chi tiết.** Nuspec 2.3.0 chỉ có ba nhóm TFM: `net6.0`, `net7.0`, **`net8.0`**. **Không có `net9.0`.** Câu "asset net8.0/net9.0" gộp chung YARP với SqlClient là không đúng cho YARP — YARP cao nhất chỉ tới `net8.0`. Không đổi kết luận (asset `net8.0` chạy tốt trên .NET 10), nhưng là chi tiết sai trong một tài liệu người ta sẽ trích lại.

**(d) Điều tài liệu chưa nói: 2.3.0 phát hành 2025-02-27 — đã ~19 tháng.** Và theo roadmap, **2.2 đã hết hỗ trợ từ 2025-08-27**, nên 2.3 là **dòng duy nhất còn được hỗ trợ**. YARP là cổng vào của toàn hệ (mọi lưu lượng đi qua nó theo sơ đồ topo) mà lại là mắt xích tiến chậm nhất trong Stack — đáng một dòng ghi nhận.

**Nguồn:**
- https://api.nuget.org/v3-flatcontainer/yarp.reverseproxy/index.json
- https://api.nuget.org/v3-flatcontainer/yarp.reverseproxy/2.3.0/yarp.reverseproxy.nuspec
- https://github.com/dotnet/yarp/blob/main/docs/roadmap.md
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/getting-started?view=aspnetcore-10.0

**Sửa cụ thể:** Tách footnote làm hai câu cho đúng:

> `Yarp.ReverseProxy 2.3.0` (phát hành 27/02/2025) có TFM cao nhất là `net8.0`; Microsoft Learn cho ASP.NET Core 10 xác nhận "YARP 2.3.0 supports .NET 8 or later". `Microsoft.Data.SqlClient 7.0.3` có TFM cao nhất là `net9.0` và được Microsoft công bố là compile/test trên .NET 10 SDK. Cả hai chạy trên .NET 10 qua asset tương thích ngược.

Và thêm vào **Deferred**: *"Theo dõi bản YARP kế tiếp — 2.3 là dòng duy nhất còn hỗ trợ và đã 19 tháng tuổi."*

---

### 10. LOW — `react-router-dom` chỉ là gói vỏ

**Tài liệu nói:** `react-router-dom | 7.18.4`.

**Tôi tìm thấy:** 7.18.4 đúng là bản mới nhất và **không bị đánh dấu deprecated**. Nhưng nội dung gói chỉ là vỏ bọc — `dependencies` của nó đúng một dòng:

```json
{ "react-router": "7.18.4" }
```

Từ React Router v7, mọi thứ nằm ở `react-router`; `react-router-dom` được giữ lại làm cầu tương thích cho code v6. Dự án mới nên import thẳng từ `react-router`.

**Nguồn:** https://registry.npmjs.org/react-router-dom

**Sửa cụ thể:** Đổi dòng Stack thành `react-router | 7.18.4`. Mức thấp, nhưng bảng Stack là thứ người ta copy vào `package.json`, và pin gói vỏ sẽ đẻ ra thói quen import sai ngay từ file đầu tiên.

---

### 11. LOW — Hạ tầng xuất hiện trong sơ đồ nhưng vắng mặt trong Stack

**Tài liệu nói:** Mục Structural Seed nêu **HAProxy**, **Redis**, **Jenkins**, **IIS qua ASP.NET Core Module**, **6 máy chủ sẵn có**. Bảng Stack không có dòng nào cho các thứ này.

Trong đó **phiên bản Redis server** là thứ thật sự quan trọng, vì nó gắn trực tiếp với mục 3: SE.Redis 3.x thương lượng RESP3 và hành vi phụ thuộc phiên bản server. HAProxy cũng đáng ghi vì tài liệu đã tự nhận nó là **điểm chết đơn lẻ** trong bảng Deferred — một thành phần được nêu đích danh là rủi ro thì nên biết nó đang chạy bản nào.

**Sửa cụ thể:** Bổ sung vào bảng Stack: phiên bản Redis server, HAProxy, Windows Server + IIS, và ASP.NET Core Module (ANCM v2, đi kèm .NET 10 Hosting Bundle). Không cần dài — cần **có số**.

---

### 12. INFO — Ngày hết hỗ trợ .NET: **xác nhận đúng**

**Tài liệu nói:** *".NET | 10 (LTS, hỗ trợ tới 14/11/2028)"* và AD-1: *".NET 8 hết hỗ trợ 10/11/2026 nên không được dùng cho code mới."*

**Tôi tìm thấy:** cả hai **đều đúng**.

- .NET 10 là **LTS**, hỗ trợ từ 11/11/2025 tới **14/11/2028** (đúng như `dotnet/core` release-notes; một số nguồn thứ cấp ghi 10/11/2028 — tài liệu đã lấy đúng con số của repo chính thức).
- .NET 8 **và .NET 9** cùng hết hỗ trợ **10/11/2026** — Microsoft đã kéo dài STS từ 18 lên 24 tháng nên hai mốc trùng nhau. Lập luận của AD-1 vững.

Bổ sung một dữ kiện tài liệu nên biết: **.NET 11 đã ở giai đoạn RC** — trên NuGet có `Microsoft.AspNetCore.SignalR.Client 11.0.0-rc.1.26425.128`. .NET 11 sẽ ra tháng 11/2026 và là **STS** (hết hỗ trợ khoảng 11/2027, trước cả .NET 10). Chọn .NET 10 LTS là **đúng**, nhưng nên ghi thêm một câu vào AD-1 để ngăn người ta "nâng cho mới": *"Không nhảy sang .NET 11 — đó là STS, hết hỗ trợ trước .NET 10."* Nếu không nói, gói preview 11.x sẽ tự xuất hiện trong danh sách gợi ý của NuGet suốt mùa thu này.

**Nguồn:**
- https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md
- https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/
- https://api.nuget.org/v3-flatcontainer/microsoft.aspnetcore.signalr.client/index.json

---

### 13. INFO — Các pin còn lại: **xác nhận đúng toàn bộ**

Kiểm trực tiếp trên registry, không có mục nào lạc hậu:

| Gói | Pin trong tài liệu | Mới nhất thực tế | Ngày | TFM `net10.0`? |
|---|---|---|---|---|
| Dapper | 2.1.86 | 2.1.86 | — | **có** |
| Serilog.AspNetCore | 10.0.0 | 10.0.0 | — | **có** |
| StackExchange.Redis | 3.2.1 | 3.2.1 | 2026-09-13 | **có** |
| Microsoft.AspNetCore.SignalR.Client | 10.0.12 | 10.0.12 | — | n/a |
| React | 19.3.0 | 19.3.0 | 2026-09-09 | — |
| Vite | 8.3.0 | 8.3.0 | 2026-09-10 | — |
| TypeScript | 7.0.2 | 7.0.2 | 2026-07-08 | — |
| @tanstack/react-query | 5.103.1 | 5.103.1 | 2026-09-16 | — |
| react-router-dom | 7.18.4 | 7.18.4 | — | — |

Không gói nào bị deprecated. Không gói nào có dự án kế nhiệm. `@tanstack/react-query` vẫn ở dòng 5 (peer `react: ^18 || ^19` — khớp React 19.3.0), chưa có v6.

Một điểm sáng đáng ghi nhận: **Dapper, Serilog.AspNetCore và StackExchange.Redis đều đã có TFM `net10.0` thật** — không phải chạy nhờ tương thích ngược. Chỉ YARP và SqlClient là chạy qua asset cũ, và cả hai đều đã được xác nhận là chạy được (mục 6, 9).

Ngoài ra, **RFC 9457** trong AD-8 và mục Conventions là **đúng số hiệu** — RFC 9457 là bản thay thế RFC 7807 cho Problem Details.

---

## Việc cần làm, theo thứ tự

**Chặn — làm trước khi ai đó dựng code:**

1. Quyết chuyện **TypeScript**: hạ về 5.9.2, hoặc giữ 7.0.2 và ghi rõ là không có type-aware lint (mục 1).
2. Sửa **bảng Stack phần SignalR**: bỏ `SignalR.Client`, thêm `SignalR.StackExchangeRedis 10.0.12` + `@microsoft/signalr 10.0.11` (mục 2).
3. Quyết chuyện **StackExchange.Redis**: hạ về 2.8.x, hoặc giữ 3.2.1 kèm bài kiểm tích hợp backplane gắn vào AD-15 (mục 2, 3).

**Nên làm trong cùng lượt sửa:**

4. Pin `grate 2.1.6`, và **chép đúng tên thư mục** từ tài liệu grate vào AD-10 (mục 4).
5. Điền phiên bản + mức CU **SQL Server** thật, thêm việc xác minh vá CVE-2026-21262 (mục 5).
6. Thêm quy tắc `Encrypt` / `TrustServerCertificate` vào AD-14 (mục 6).
7. Pin **Node.js 24 LTS** (mục 7).
8. Pin **OpenTelemetry 1.18.0** + exporter, và phân vai rõ với Serilog (mục 8).
9. Viết lại **footnote TFM** cho đúng, thêm nguồn Microsoft Learn (mục 9).
10. Đổi sang `react-router 7.18.4` (mục 10).
11. Bổ sung hạ tầng (Redis server, HAProxy, IIS/ANCM) vào Stack (mục 11).
12. Thêm câu "không nhảy sang .NET 11 (STS)" vào AD-1 (mục 12).

---

## Ghi chú về phương pháp

Những gì đã kiểm trực tiếp qua API, không qua trí nhớ mô hình:

- Danh sách phiên bản NuGet: `https://api.nuget.org/v3-flatcontainer/{pkg}/index.json`
- TFM và phụ thuộc: `https://api.nuget.org/v3-flatcontainer/{pkg}/{ver}/{pkg}.nuspec` — đây là cách duy nhất xác định chắc chắn một gói có `net10.0` hay không, và là cách phát hiện ra xung đột ở mục 2.
- Phiên bản, `engines`, `peerDependencies` npm: `https://registry.npmjs.org/{pkg}` — trường `peerDependencies` là thứ lộ ra vấn đề TypeScript ở mục 1.
- GitHub Releases API cho lịch sử phát hành grate và StackExchange.Redis.
- Microsoft Learn (moniker `aspnetcore-10.0`) cho claim hỗ trợ .NET 10 của YARP.

**Hai chỗ tôi chưa xác minh xong và không muốn khẳng định thay:**

- **Tên thư mục thật của grate** (mục 4) — cần mở mục "Script types" trong tài liệu grate và chép nguyên văn.
- **Ngày EOL chính xác của Node 20** (mục 7) — khuyến nghị dùng Node 24 vẫn đứng vững nhờ hai trường `engines` đã kiểm, nhưng đừng ghi ngày EOL vào tài liệu trước khi tra nodejs.org.
