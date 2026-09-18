# Review — ARCHITECTURE-SPINE.md (WEB2)

- **Tài liệu được review:** `_bmad-output/planning-artifacts/architecture/architecture-ba_staxi_webadmin-2026-09-17/ARCHITECTURE-SPINE.md`
- **Lăng kính:** danh mục "xương sống tốt" — (1) chốt đúng điểm phân kỳ cho tầng dưới, (2) mọi Rule phải ép được, (3) Deferred không được che điểm phân kỳ sống, (4) công nghệ nêu tên phải đúng bản hiện hành, (5) phải **phê chuẩn** codebase brownfield chứ không mâu thuẫn nó, (6) mọi chiều thuộc altitude này phải **được quyết / được hoãn / hoặc là câu hỏi mở tường minh** — im lặng cả một chiều là một finding.
- **Đầu vào đã đọc:** ADR-001…004, `brainstorm-intent.md`, `AGENTS.md`.
- **Ngày:** 2026-09-17

---

## Phán quyết

**KHÔNG ĐẠT CỔNG.** Cần một vòng sửa nữa trước khi cho phép viết dòng code WEB2 đầu tiên.

Phần đã viết rất tốt: AD-3/AD-4/AD-7/AD-10 là những chốt đúng chỗ, sắc, và giải đúng ba rủi ro lớn nhất của D2. AD-11 trung thực một cách hiếm gặp. Vấn đề không nằm ở cái đã viết mà ở **cái không viết**: spine chốt rất kỹ *chiều cách ly tenant* rồi bỏ trắng gần như toàn bộ **envelope vận hành** (backup/DR, thứ tự deploy, bí mật, CI/CD, môi trường) và phần lớn **cross-cutting** (test, phiên/token, hiệu năng, lưu giữ dữ liệu). Với một tài liệu tự nhận altitude *initiative*, đó là hơn nửa diện tích trách nhiệm.

Nghiêm trọng hơn: có **một mâu thuẫn trực tiếp gây hỏng dữ liệu ngay ngày đầu chạy song song** (C1), và **hai AD mà phần bù đắp của chúng rỗng** (C2 gác một mô hình chưa ai định nghĩa; C3 bắt buộc ghi vết vào một cái kho không tồn tại).

| Mức | Số lượng |
|---|---|
| Critical | 7 |
| High | 18 |
| Medium | 14 |
| Low | 3 |

---

# CRITICAL

## C1 — "Lưu … bằng UTC ISO-8601" làm hỏng dữ liệu dùng chung, và mâu thuẫn thẳng với AD-10

**Sai ở đâu.** Bảng *Consistency Conventions*, dòng **Ngày & số**: "Lưu **và** truyền qua API bằng **UTC ISO-8601**; quy đổi sang `vi-VN` chỉ ở tầng trình bày."

**Vì sao hỏng.** WEB2 **ghi vào chính 565 bảng của web cũ**. Web cũ ép culture `vi-VN` trong `Application_Start` và ghi giờ địa phương GMT+7 vào các cột `datetime` đó (`AGENTS.md`). Từ chữ "Lưu", lập trình viên WEB2 sẽ ghi UTC vào đúng cột mà web cũ đọc như giờ địa phương → **mọi bản ghi WEB2 lệch 7 giờ** so với mọi bản ghi web cũ, trên cùng một cột, không có cờ nào phân biệt. Báo cáo gộp hai nguồn sẽ sai lặng lẽ, và không constraint nào bắt được.

Đồng thời, AD-10 **cấm** "đổi nghĩa giá trị". Ghi UTC vào một cột đang mang nghĩa giờ địa phương **chính là** đổi nghĩa giá trị. Hai mục trong cùng một tài liệu nói ngược nhau, và mục yếu hơn (bảng quy ước) là mục lập trình viên đọc hằng ngày.

**Sửa cụ thể.** Tách "lưu" khỏi "truyền":
- **Cột đang tồn tại: giữ nguyên quy ước hiện hành (giờ địa phương vi-VN).** Viết thành một AD riêng, không để trong bảng quy ước.
- **UTC ISO-8601 chỉ áp cho wire format của API và cho bảng/cột hoàn toàn mới** (cột mới phải đặt tên có hậu tố `Utc`).
- Quy đổi nằm ở **đúng một chỗ** trong `Staxi.Platform` (ví dụ `IClock` + `TenantTimeZone`); cấm `DateTime.Now`/`DateTime.UtcNow` rải rác — bổ sung vào danh sách AD-15 (test kiến trúc bắt được ngay bằng cách quét lời gọi tới hai property này ngoài adapter thời gian).
- Ghi rõ múi giờ của tenant là dữ liệu trong sổ đăng ký hãng hay hằng số GMT+7.

---

## C2 — Không có AD nào định nghĩa mô hình quyền, nhưng AD-15 lại làm đỏ build vì nó

**Sai ở đâu.** AD-15 liệt kê "có action **thiếu khai báo quyền**" là một điều kiện làm đỏ build. Bảng lớp kiến trúc ghi Application chứa "sổ đăng ký quyền". Nhưng **không có AD nào nói khai báo quyền là gì**: mã quyền là dữ liệu hay hằng, sinh ra ở đâu, `permissionVersion` tăng khi nào và do ai, một khai báo sinh ra cả chốt chặn server lẫn điều kiện ẩn/hiện nút trên React ra sao, quyền có phân cấp không, `.AcrossTenants` xin quyền gì.

**Vì sao hỏng.** Đây là **khe số 2 trong sáu khe** mà brainstorm nêu là phải vững ngay từ đầu (`PermissionSet`, tra cứu O(1), mã quyền là **dữ liệu** chứ không phải enum biên dịch cứng, khai báo một chỗ). Hiện trạng là một enum `UrlNumberCode` **5212 dòng** cộng quyền lưu thành chuỗi phẩy trong `Session` — tức là có một hệ thống cũ khổng lồ cần người kế nhiệm. Spine im lặng nghĩa là: hai lập trình viên sẽ phát minh hai mô hình quyền khác nhau trong hai tuần đầu, và AD-15 sẽ gác một thứ không tồn tại (test đó không viết nổi vì chưa biết "khai báo quyền" trông thế nào). Đây đúng là định nghĩa của "điểm phân kỳ cho tầng dưới bị bỏ sót".

**Sửa cụ thể.** Thêm **AD-16 — Mô hình quyền**, tối thiểu chốt:
1. Mã quyền là **chuỗi dữ liệu** trong bảng, không phải enum; sổ đăng ký quyền khai báo một lần trong Application và là nguồn sinh ra (a) attribute gác endpoint, (b) payload quyền mà BFF trả cho FE để ẩn/hiện.
2. `permissionVersion` là số nguyên tăng theo **(tenant, user)** hoặc theo role; ai tăng, tăng khi nào, ảnh hưởng thế nào tới cache (AD-6) và tới token đang lưu hành.
3. Quyền được đánh giá **theo `companyId` trong token** — nêu tường minh là **không port** `GetRoleLastest` của web cũ (ADR-004 chỉ ra bộ lọc `FK_CompanyID` đã biến mất ở bản đang chạy).
4. Đường di trú: 5212 dòng `UrlNumberCode` ánh xạ sang mã quyền mới bằng cách nào — bảng ánh xạ một lần, hay khai lại theo từng màn hình khi chuyển.

---

## C3 — AD-11 bắt buộc ghi vết mọi lệnh ghi, nhưng kho ghi vết không tồn tại ở bất kỳ đâu trong spine

**Sai ở đâu.** AD-11: "mọi lệnh ghi **bắt buộc ghi vết** `corrId` + tenant + user + bảng + khoá bản ghi". Bảng *Rủi ro đã chấp nhận* dựa toàn bộ vào dòng này để bù cho last-write-wins.

**Vì sao hỏng.** Không chỗ nào nói: ghi vết đi vào **bảng nào**, nằm trong **DB của hãng hay DB quản trị**, **schema** ra sao, **giữ bao lâu** (một hệ điều vận taxi ghi rất dày — đây là bài toán dung lượng thật), **ai được đọc**, và **ép bằng gì** (AD-15 không có mục nào cho nó). Một Rule không có cơ chế và không có chốt chặn là một lời khuyên — đúng thứ AD-15 tuyên bố sẽ ngăn.

Hệ quả nặng nhất: rủi ro **mất dữ liệu âm thầm** đã được chấp nhận có ý thức, và **thứ duy nhất** đổi lại là ghi vết. Nếu ghi vết không được đặc tả, thì rủi ro đã được chấp nhận **mà không nhận được gì**.

**Sửa cụ thể.**
- Chốt kho: khuyến nghị **bảng mới trong DB của chính hãng** (AD-10 cho phép thêm bảng mới; giữ ghi vết cùng transaction với lệnh ghi nghiệp vụ là thứ duy nhất đảm bảo không lệch). Nêu tên bảng, cột, index, và chính sách phân vùng/đẩy sang lưu trữ lạnh.
- Chốt **retention** (ví dụ: 90 ngày online, 2 năm lưu trữ) — đây cũng là chiều "data retention" mà spine đang im lặng hoàn toàn.
- Chốt cơ chế: ghi vết phát sinh ở **một chỗ** (interceptor trong Infrastructure/repository), không phải mỗi use case tự gọi.
- Thêm vào AD-15: mọi lời gọi `Execute`/`ExecuteAsync` của Dapper phải đi qua wrapper có ghi vết — test tham chiếu assembly hoặc test quét lời gọi bắt được việc này.
- Xem thêm **H1**: ghi vết chỉ của WEB2 là ghi vết một nửa.

---

## C4 — Vòng đời phiên: hạn token, nơi FE lưu token, refresh, đăng xuất, thu hồi — im lặng; và "stateless" tự mâu thuẫn

**Sai ở đâu.** Bảng quy ước, dòng **Xác thực**: "JWT stateless, mang `SecurityStamp` + `permissionVersion`, **kiểm lại mỗi request**; **không dùng server session**."

**Vì sao hỏng.** Hai lỗi trong một dòng:
1. **Mâu thuẫn.** "Kiểm lại mỗi request" nghĩa là mỗi request phải đối chiếu `SecurityStamp`/`permissionVersion` với một nguồn sự thật → một lượt Redis hoặc một lượt SQL cho **mỗi** request. Đó không phải stateless; đó là session có tên khác. Nếu định làm vậy thì phải nói rõ nguồn là Redis, khoá dựng theo AD-6 nào, TTL bao nhiêu, và fail-open hay fail-closed khi Redis mất — vì fail-open ở đây nghĩa là token bị thu hồi vẫn dùng được.
2. **Im lặng ở phần nguy hiểm nhất.** Không có: thời hạn access token, có refresh token không và lưu ở đâu, **FE lưu token ở đâu** (spine chỉ dặn lưu *mã hãng* trong `localStorage`). Không chốt thì đội FE sẽ mặc định `localStorage` cho cả token, và mọi lỗ XSS trở thành lỗ chiếm phiên toàn hãng. Không có luồng đăng xuất, không có "đăng xuất mọi thiết bị", không có xử lý token cấp trước khi đổi công ty (AD-4 cấp token mới — token cũ còn sống bao lâu?).

**Sửa cụ thể.** Thêm **AD-17 — Vòng đời phiên**: access token ngắn (10–15 phút) + refresh token trong **cookie `HttpOnly`, `Secure`, `SameSite=Strict`** trên domain gateway; access token **chỉ giữ trong bộ nhớ JS**, không `localStorage`/`sessionStorage`; danh sách thu hồi (`SecurityStamp`/`permissionVersion`) trong Redis với TTL = hạn token, **fail-closed**; đổi công ty ⇒ token cũ vào danh sách thu hồi ngay; luồng đăng xuất xoá cả cookie lẫn group SignalR (bổ trợ AD-7).

---

## C5 — Envelope vận hành: backup/DR, thứ tự deploy migration ↔ app, rollback — trắng hoàn toàn

**Sai ở đâu.** Toàn bộ chiều vận hành gói trong **một câu**: "Triển khai trên 6 máy chủ sẵn có, IIS qua ASP.NET Core Module, deploy bằng Jenkins. Môi trường: dev · devtest · staging · production." Không có AD nào bind vào nó.

**Vì sao hỏng.** Ba lỗ hổng cụ thể, không phải lời phàn nàn chung chung:

1. **DB quản trị (sổ đăng ký hãng) là một database MỚI, là SPOF được thừa nhận, và không có một chữ nào về sao lưu/khôi phục.** AD-14 chỉ bù bằng cache cục bộ + health check — cả hai đều chỉ chịu được **mất tạm thời**. Mất *dữ liệu* (hỏng file, xoá nhầm dòng, mất khoá mã hoá) thì **18 hãng không đăng nhập được và không có đường về**. Phải có: chế độ recovery, tần suất backup, RPO/RTO, và **bài tập khôi phục đã chạy thử** trước khi hãng thí điểm lên.
2. **Thứ tự deploy giữa migration và app không được chốt.** Toàn bộ an toàn của AD-10 dựa vào expand → migrate → contract, nhưng spine không nói script grate chạy **trước** khi deploy app hay sau, chạy tự động trong Jenkins hay do DBA chạy tay, chạy lần lượt 18 DB hay song song, và điều gì xảy ra khi DB thứ 7 lỗi giữa chừng. Đây là điểm phân kỳ kinh điển: hai người sẽ làm hai kiểu và một ngày nào đó app mới gặp DB chưa migrate.
3. **Rollback không tồn tại.** ADR-004 có một câu vàng — "lùi về = bảo người dùng quay lại URL cũ, không có bước kỹ thuật nào" — và spine **đánh rơi nó**. Đó là tài sản lớn nhất của kiến trúc Strangler Fig này và đáng được phê chuẩn thành luật (kèm điều kiện: chỉ đúng khi schema còn tương thích ngược, tức là rollback app được nhưng rollback migration thì không).

**Sửa cụ thể.** Thêm mục **"Envelope vận hành"** với ít nhất: AD về thứ tự phát hành (migration chạy trước, idempotent, dừng ở DB lỗi, báo cáo DB nào ở mức nào); AD về rollback (app lùi được, DDL không lùi — do đó chỉ thêm, không sửa); chính sách backup/DR cho DB quản trị và cho khoá mã hoá; health/readiness endpoint mà HAProxy dùng; ai bấm deploy.

---

## C6 — Khoá mã hoá connection string và khoá ký JWT không có nhà

**Sai ở đâu.** AD-14: "Connection string **mã hoá trong cột**, khoá nằm ngoài mã nguồn." Bảng quy ước: "Không có bí mật trong mã nguồn hay trong git."

**Vì sao hỏng.** "Ngoài mã nguồn" không phải một quyết định — nó loại trừ đúng một khả năng và để mở tất cả phần còn lại. ADR-004 đã đặt ba phương án lên bàn (**Azure Key Vault / HashiCorp Vault / DPAPI+Redis**) và để mở; spine **không chốt, không hoãn, cũng không ghi thành câu hỏi mở**. Trên nền on-prem 6 máy IIS, ba phương án đó khác nhau một trời một vực về vận hành. Chưa kể còn các bí mật khác chưa ai nhắc: **khoá ký JWT** (và chia sẻ nó giữa N instance — trên ASP.NET Core đây cũng là bài toán Data Protection key ring, ADR-004 đã nhắc "Data Protection trên Redis"), mật khẩu Redis, credential đẩy file lên CDN, credential Jenkins tới 18 DB.

**Vì sao là Critical chứ không phải High.** Không chốt được chỗ giữ khoá thì **không dựng được sổ đăng ký hãng**, mà sổ đăng ký hãng là tiền đề của màn đăng nhập, tức là của mọi thứ. Nó chặn đường tới hạng mục đầu tiên.

**Sửa cụ thể.** Chốt một phương án và viết ra: nơi giữ khoá, ai đọc được, **cách xoay khoá** (xoay khoá mà 18 connection string đang mã hoá bằng nó là một quy trình, không phải một nút bấm), chuyện gì xảy ra khi kho khoá tạm mất (AD-14 nói phải chạy tiếp được — cache cục bộ giữ connection string đã **giải mã** hay còn mã hoá? Nếu đã giải mã thì cache đó cũng là bí mật và cũng cần chính sách). Data Protection key ring cho N instance phải có tên trong Stack.

---

## C7 — Số phận 6 thư viện dùng chung và 4 WCF Service Reference: im lặng ở đúng altitude phải trả lời

**Sai ở đâu.** Spine mô tả cây nguồn `web2/` như một thế giới hoàn toàn mới (`Staxi.*`), và **không nói một chữ** về `BA.STaxi.{Core,Common,Data,Repository,Service,SystemAdapter}`.

**Vì sao hỏng.**
- Brainstorm đã **đo** rằng 6 thư viện này gần như sạch `System.Web` (**15/1766 file**) và "port được" — đó là phát hiện khả thi quan trọng nhất của cả phiên. Spine không phê chuẩn cũng không bác bỏ nó. Đây là câu hỏi quyết định **khối lượng công việc theo cấp số nhân**: dùng lại `BA.STaxi.Service` hay viết lại nghiệp vụ trong `Staxi.Admin.Application`? Hai lập trình viên sẽ chọn hai đường trong tuần đầu, và đường nào cũng "đúng theo spine".
- Nặng hơn: `BA.STaxi.SystemAdapter` có **4 Service Reference WCF** tới hệ thống ngoài. `.NET 10` không có WCF client như .NET Framework — phải sinh lại bằng `dotnet-svcutil`/CoreWCF. Phạm vi đợt đầu có **Dashboard và Online (SignalR)**, tức là gần như chắc chắn phải nói chuyện với hệ điều vận. Vậy mà **cả chiều "tích hợp hệ thống ngoài" không tồn tại trong spine**: không AD, không dòng Stack, không Deferred, không câu hỏi mở. Cộng thêm `HubCenter` (`/Center/`) mà ADR-002 nêu là thuộc hệ thống khác và chưa rõ chủ.

**Sửa cụ thể.** Thêm một AD "Biên giới với mã nguồn cũ và hệ thống ngoài": (a) WEB2 **không tham chiếu** assembly .NET Framework nào — nếu tái sử dụng thì tái sử dụng bằng cách **chép mã nguồn vào project mới** có kiểm tra, chứ không tham chiếu chéo (nêu rõ để test kiến trúc ép được); (b) liệt kê 4 WCF endpoint, chốt CoreWCF/`dotnet-svcutil` hay bọc lại bằng REST, và ai sở hữu chúng; (c) `HubCenter` là câu hỏi mở có người phụ trách.

---

# HIGH

## H1 — Deferred "Nối log WEB2 với log web cũ" là một điểm phân kỳ SỐNG, không phải việc hoãn được

**Sai ở đâu.** Bảng *Deferred*: hoãn nối log, lý do "hai trang web riêng, người dùng không đi qua lại trong một phiên". AD-13 nói thẳng: "`corrId` của WEB2 là mã riêng, **không** cố nối với định dạng log của web cũ."

**Vì sao đây là finding.** Lý do hoãn nói về **phiên người dùng**, nhưng vấn đề không nằm ở phiên — nó nằm ở **dữ liệu**. Hai ứng dụng **ghi vào cùng một hàng của cùng một bảng**. AD-11 chấp nhận mất dữ liệu âm thầm và bù bằng ghi vết. Nhưng nếu chỉ WEB2 ghi vết, thì khi một bản ghi bị đè, ta trả lời được "WEB2 đã ghi gì" và **không trả lời được** "ai đè lên nó" trong đúng nửa số trường hợp — và nửa đó (web cũ đè lên WEB2) chính là nửa dễ xảy ra nhất trong giai đoạn đầu, khi 95% thao tác vẫn ở web cũ.

Ngoài ra brainstorm (khe #5) nói ngược lại: "Web cũ và web mới dùng **chung không gian corrId**", và web cũ **đã có sẵn** `RequestTracer` + sink `[Admin.RequestTrace]` + `CustomJsonLayout` phát host/app/corrId/url/companyid. Tức là hạ tầng đã có, spine đảo chiều quyết định mà không ghi nhận mình đang đảo chiều.

**Sửa cụ thể.** Bỏ dòng này khỏi Deferred. Yêu cầu tối thiểu: **bảng ghi vết (C3) có cột `AppId`**, và **web cũ được sửa để ghi vào cùng bảng đó cho các bảng nghiệp vụ mà cả hai cùng chạm** (D6 cho phép sửa web cũ thoải mái; `RequestTracer` đã có corrId nên chi phí thấp). Định dạng log có thể khác nhau — **sổ ghi vết thì không được**.

## H2 — Mâu thuẫn: Deferred nói "chưa tách dịch vụ", Structural Seed đã tách sẵn 6 deployable

**Sai ở đâu.** Deferred: "Tách BE thành nhiều dịch vụ — chưa có gì để tách; BFF đã đủ chỗ gom". Nhưng cây nguồn liệt kê `Staxi.Auth.Api`, `Staxi.Admin.Api`, `Staxi.Reporting.Api`, `Staxi.Notification.Api`, `Staxi.Bff.Web`, `Staxi.Gateway` — sáu project có hậu tố `.Api`, và topo vẽ chúng như các node riêng.

**Vì sao hỏng.** Tầng dưới cần biết chính xác: đây là **6 tiến trình** (6 site IIS, 6 app pool, 6 cấu hình, 6 health check, gọi nhau qua HTTP) hay **1 tiến trình host nhiều module** (gọi nhau in-process)? Câu trả lời quyết định: cấu hình YARP, cách BFF gọi API (HttpClient có retry/timeout/circuit breaker hay lời gọi hàm), số connection pool (xem H7), cách truyền `traceparent`, và cách deploy. Hiện spine cho phép cả hai cách đọc.

**Sửa cụ thể.** Chốt một câu trong Structural Seed: "Đợt đầu chạy **N tiến trình**: …". Nếu là mô-đun trong một host thì đổi hậu tố project cho khớp (`.Module` thay vì `.Api`) để tên không nói dối. Nếu là nhiều tiến trình thì bỏ dòng Deferred kia đi vì việc tách **đã xảy ra rồi**.

## H3 — AD-15 bỏ trống chốt chặn cho AD-3, AD-4, AD-8, AD-10, AD-11, AD-12; và AD-9/AD-12 viết ở dạng máy không kiểm được

**Sai ở đâu.** AD-15 liệt kê 7 điều kiện làm đỏ build. Đối chiếu với 14 AD còn lại:

| AD | Có chốt chặn trong AD-15? |
|---|---|
| AD-2 (`new SqlConnection`) | ✅ nhưng dễ lách — xem dưới |
| AD-3 (cấm endpoint liệt kê hãng) | ❌ |
| AD-4 (cấm nhận `tenantId`/`companyId` từ request) | ❌ **— chính là AD ngăn lỗ hổng `ChangeCompany`** |
| AD-5, AD-6, AD-7 | ✅ |
| AD-8 (không trả 200 cho lỗi, ProblemDetails) | ❌ |
| AD-9 (đường biên proc/.NET) | ❌ và không thể kiểm như đang viết |
| AD-10 (DDL chỉ thêm) | ❌ |
| AD-11 (mọi lệnh ghi có ghi vết) | ❌ |
| AD-12 (BFF không chứa nghiệp vụ) | ❌ và không thể kiểm như đang viết |
| AD-13 | ❌ |

**Vì sao hỏng.** AD-15 tự tuyên bố nó là thứ ngăn "mọi AD ở trên thoái hoá thành lời khuyên". Trên thực tế nó chỉ đỡ 4/14. Đáng nói nhất là **AD-4 không có chốt chặn** — AD-4 là AD ngăn đúng lỗ hổng leo thang tenant mà spine đã bỏ công chỉ mặt trong web cũ.

**Sửa cụ thể.** Bổ sung vào AD-15:
- **AD-4:** quét mọi tham số action + mọi property của DTO request; tên khớp `(?i)(tenant|company)_?id` → đỏ. Rẻ và chặn đúng lỗi.
- **AD-12:** đổi Rule sang dạng cơ học — `Staxi.Bff.Web` **cấm tham chiếu** bất kỳ assembly `*.Domain`, `*.Infrastructure`, `Dapper`, `Microsoft.Data.SqlClient`. "Cấm chứa quy tắc nghiệp vụ" không test được; "cấm tham chiếu" thì test được trong 5 dòng.
- **AD-2:** kiểm ở mức **tham chiếu assembly** thay vì tên kiểu — cấm dùng namespace `Microsoft.Data.SqlClient` ngoài đúng project chứa `ITenantConnectionFactory`. Cách viết hiện tại ("cấm `new SqlConnection`") bị `DbProviderFactory.CreateConnection()` lách qua mà vẫn xanh.
- **AD-8:** test quét endpoint — cấm `return Ok(...)` trong nhánh `catch`; bắt buộc mọi endpoint khai báo `ProducesProblemDetails`; một test tích hợp ném lỗi thật và khẳng định status ≠ 200 và `Content-Type: application/problem+json`.
- **AD-10:** linter trên `db/migrations/**.sql` — bắt `DROP COLUMN`, `ALTER COLUMN`, `sp_rename` → đỏ, trừ khi file có dòng chú thích miễn trừ kèm lý do.
- **AD-11:** xem C3.

## H4 — Khoá cache thiếu tiền tố môi trường, và không có chính sách TTL/vô hiệu hoá

**Sai ở đâu.** AD-6 khuôn khoá: `{hãng}:{companyId}:{vùng}:{khoá}`. AD-7 backplane: tiền tố `{môi-trường}:{hãng}`.

**Vì sao hỏng.** Hai chỗ, hai khuôn. **Backplane có `{môi-trường}` còn cache thì không.** Trên 6 máy on-prem với 4 môi trường, khả năng cao staging và production dùng chung một Redis (hoặc chung instance khác số DB — nhưng spine không nói). Nếu chung, một lần chạy thử ở staging sẽ **ghi đè cache production của đúng hãng đó**, và AD-6 chống rò rỉ giữa hãng với hãng nhưng không chống rò rỉ giữa môi trường với môi trường — một dạng rò rỉ tệ hơn, vì dữ liệu thử nghiệm hiện ra trên màn hình khách thật.

Thêm nữa, spine không nói gì về **TTL và vô hiệu hoá**. Đây đúng là nỗi đau đã đo của hệ cũ: "`ICache` chỉ có `Put`/`Get` — không TTL, không invalidate". Xây lại `CacheKey` an toàn tenant mà vẫn không có TTL là sửa nửa vấn đề.

**Sửa cụ thể.** Một khuôn duy nhất cho cả cache lẫn backplane: `{môi-trường}:{tenantCode}:{companyId}:{vùng}:{khoá}`, sinh bởi **một hàm duy nhất**, và `CacheKey` không dựng được nếu thiếu môi trường (cùng cơ chế đã dùng cho tenant). Bổ sung: mỗi `{vùng}` phải khai báo TTL mặc định và sự kiện vô hiệu hoá; cấm cache không TTL.

## H5 — grate: tên thư mục sai, không ghim phiên bản, và để tồn tại hai sổ cái migration trên cùng 18 DB

**Sai ở đâu.** AD-10: "chạy bằng **grate** — sprocs/views/functions đặt ở thư mục **`AnyTime`**". Cây nguồn: `db/migrations/  # grate: Once / AnyTime / EveryTime`. Deferred: hoãn "di trú sổ cái `[Admin.DbMigrations]` sang grate".

**Vì sao hỏng.**
1. **grate không có thư mục nào tên `AnyTime`, `Once` hay `EveryTime`.** Đó là *kiểu chạy* thời RoundhousE. grate dùng các thư mục có tên cố định, chạy theo thứ tự: `beforeMigration`, `alterDatabase`, `runBeforeUp`, **`up`**, `runFirstAfterUp`, **`functions`**, **`views`**, **`sprocs`**, `triggers`, `indexes`, `runAfterOtherAnyTimeScripts`, `permissions`, `afterMigration` — mỗi thư mục có chế độ `once` / `on_change` / `always`. Viết như spine đang viết thì đội sẽ tạo ba thư mục mà grate **bỏ qua hoàn toàn**, và không script nào chạy. Đây là lỗi sự kiện kiểm chứng được, không phải ý kiến.
2. **grate tự tạo bảng sổ cái riêng của nó** (schema `grate`: `ScriptsRun`, `ScriptsRunErrors`, `Version`) trong **từng** database nó chạm. Nghĩa là nó sẽ tạo bảng mới trong 18 DB khách hàng — AD-10 cho phép (thêm bảng mới), nhưng phải nói ra, và phải nói với DBA trước.
3. Cộng (2) với dòng Deferred, kết quả là **hai sổ cái migration cùng sống trên 18 DB khách**: `[Admin.DbMigrations]` (30 dòng, có checksum, do một hệ khác ghi, **chưa biết chủ**) và sổ cái của grate. Hai bộ chạy script không biết nhau, trên cùng một schema. ADR-003 đã cảnh báo đúng nguy cơ này ("nếu không sẽ có hai bộ cùng ghi vào một bảng") và spine hoãn đúng cái nó cảnh báo. Đây là một mục Deferred che một điểm phân kỳ sống (lăng kính #3).
4. Dòng "*Đã kiểm chứng trên NuGet và npm ngày 2026-09-17*" đứng ngay trên một bảng mà **grate không có số phiên bản**.

**Sửa cụ thể.** Sửa AD-10 và cây nguồn sang tên thư mục thật của grate; ghim phiên bản grate; nêu rõ grate tạo schema riêng trong DB khách. Và **bỏ dòng Deferred**: thay bằng một câu hỏi mở **có người phụ trách và hạn** — "ai sở hữu `[Admin.DbMigrations]`?" — với luật tạm thời: **cho tới khi trả lời được, không script grate nào được chạy lên DB khách hàng**; chỉ chạy trên DB dev.

## H6 — Kiểm kê trôi schema 18 DB biến mất, làm việc hoãn ADR-001 trở thành một canh bạc mù

**Sai ở đâu.** ADR-003 xếp **bước 1** là: "Kiểm kê trôi schema trước tiên… Rẻ, làm được ngay, và **nó quyết định mọi thứ phía sau**. Hiện không ai biết con số này." Spine **không nhắc tới nó ở bất kỳ đâu** — không AD, không Deferred, không câu hỏi mở.

**Vì sao hỏng.** Spine hoãn cơ chế lệch phiên bản (ADR-001) với lý do "đợt đầu chỉ có vài màn hình; lệch phiên bản chưa phát sinh". Lý do đó **chỉ đúng nếu 18 DB đang gần giống nhau** — và ADR-003 nói thẳng là không ai biết chúng lệch nhau bao nhiêu. Hoãn một cơ chế bảo vệ dựa trên một giả định mà tài liệu nguồn ghi rõ là **chưa đo**, đó không phải hoãn, đó là đánh cược. Nếu hãng thí điểm có một proc đã bị sửa tay khác 17 hãng kia, mọi màn hình chuyển sang sẽ vỡ ở hãng thứ hai, đúng lúc không ai còn nhớ vì sao.

**Sửa cụ thể.** Đưa **kiểm kê trôi schema** vào spine như điều kiện tiên quyết của đợt đầu (nó là truy vấn chỉ-đọc trên `sys.tables`/`sys.columns`/`sys.procedures`, `AGENTS.md` cho phép chạy thẳng). Sửa điều kiện xem lại của dòng Deferred ADR-001 từ "khi hãng thứ hai lên WEB2" thành "**trước khi hãng thứ nhất lên WEB2**, dựa trên kết quả kiểm kê" — và bổ sung một chốt rẻ tiền mà spine đang thiếu: **API kiểm tra schema lúc khởi động** (khẳng định các đối tượng nó cần tồn tại trong DB tenant, từ chối phục vụ tenant đó nếu không) — rẻ hơn nhiều so với toàn bộ cơ chế `[Version.ApiSupported]`, và bắt được 90% ca.

## H7 — Bài toán connection pool 18 × N biến mất khỏi spine

**Sai ở đâu.** ADR-004 tính rõ: "Trước: 1 site = 1 DB. Giờ **1 app × 18 DB × N instance**… `Max Pool Size` mặc định 100 ⇒ `18 × 6 × 100 =` **10.800 kết nối tiềm năng** về phía SQL. Phải đặt có chủ đích và giám sát." Spine: không một chữ.

**Vì sao hỏng.** Đây là hệ quả trực tiếp và định lượng được của AD-2 (một deployment, 18 DB) — tức là đúng loại điểm phân kỳ mà spine sinh ra để chốt. Không chốt thì: mỗi dịch vụ tự chọn `Max Pool Size`, sổ đăng ký hãng trả về connection string không có tham số pool, và ngày SQL Server từ chối kết nối sẽ không ai truy được vì sao. Nó cũng là đầu vào bắt buộc cho H2 (mỗi tiến trình thêm vào là một bộ 18 pool nữa).

**Sửa cụ thể.** Một AD hoặc một dòng trong AD-14: connection string trong sổ đăng ký **phải** chứa `Max Pool Size`, `Min Pool Size`, `Connect Timeout` do nền tảng đặt (không để hãng tự viết); nêu con số trần cho mỗi tiến trình và tổng; metric số kết nối đang mở là metric bắt buộc trong AD-13.

## H8 — `.AcrossTenants` nhập nhằng tenant/company; tập phạm vi uỷ quyền và luật "không có giá trị ma thuật" bị đánh rơi

**Sai ở đâu.** AD-5: "Muốn vượt phạm vi phải gọi tường minh `.AcrossTenants(lyDo)`". AD-4: token mang **một** `tenant` + **một** `companyId`.

**Vì sao hỏng.**
1. **Tên nói dối.** AD-5 nói về việc lọc `CompanyId` — tức là vượt qua ranh giới **công ty bên trong một DB hãng**. Nhưng tên `.AcrossTenants` nói là vượt qua ranh giới **hãng**, mà vượt ranh giới hãng nghĩa là mở một connection khác (AD-2), điều mà repository không làm được vì nó đã bị gắn với một connection. Lập trình viên sẽ hiểu sai theo cả hai chiều. Tách thành hai khái niệm và hai tên: `.AcrossCompanies(lyDo)` (trong một DB) và một đường riêng, hiếm, có quyền riêng cho việc đọc chéo hãng — hoặc **cấm hẳn** đọc chéo hãng và nói ra.
2. **Mất tập phạm vi.** Khe #1 của brainstorm yêu cầu `TenantContext` giữ **một tập** phạm vi được uỷ quyền, chính vì có admin cấp nhóm G7 xem nhiều công ty — và repo này **đang có** `BA.STaxi.LandingPage`, tức là admin cấp nhóm đã tồn tại thật. AD-4 với một `companyId` duy nhất trong token không phục vụ được ca đó, và spine không nói ca đó nằm ở đâu (ở lại web cũ? hay dùng `.AcrossTenants`?).
3. **Mất luật "không có giá trị ma thuật".** Brainstorm chốt: "Không có giá trị ma thuật (`CompanyId = 0`); `AllTenants` là một kiểu riêng". Luật này biến mất. Nó rẻ, ép được, và ngăn đúng một họ lỗi kinh điển của codebase này.

**Sửa cụ thể.** Viết lại AD-5 với ba khái niệm phân biệt (một công ty / nhiều công ty trong một hãng / chéo hãng), nêu ca admin nhóm thuộc loại nào, và khôi phục luật "AllTenants là một kiểu, không phải số 0" vào AD-5 + AD-15.

## H9 — Không có mục "Câu hỏi mở", và spine trình bày 4 ADR *chưa chốt* như đã chốt

**Sai ở đâu.** Cả bốn ADR đều ghi **"Trạng thái: Đề xuất (chưa chốt)"**, và mỗi ADR kết bằng một mục "Việc còn mở". Spine tham chiếu chúng trong `sources` rồi viết mọi thứ ở thể khẳng định, và **không có mục nào cho câu hỏi mở**.

Các câu hỏi bị bốc hơi, tất cả đều còn sống: ai sở hữu `[Admin.DbMigrations]` và `[Version.ApiSupported]`; `ApiGroup` hiện mang nghĩa gì; `HubCenter` do hệ nào sở hữu và đã có khái niệm tenant chưa; vì sao `ConnectSignalr` đang bị comment; màn hình Online cần độ trễ bao nhiêu và bao nhiêu xe mỗi tenant lúc cao điểm (câu này quyết định SignalR hay SSE); trong 2.003 proc bao nhiêu cái còn được gọi; **domain mới tên gì, cert ở đâu, ai cấp**; HAProxy đã có cặp active-passive chưa.

**Vì sao hỏng.** Lăng kính #6: một chiều phải **được quyết, được hoãn, hoặc là câu hỏi mở tường minh**. "Không nhắc tới" là trạng thái thứ tư và là trạng thái duy nhất không chấp nhận được. Riêng **domain + chứng chỉ TLS** là chiều hạ tầng bắt buộc: không có domain thì không có màn đăng nhập, và AD-3 (mã hãng, không lộ danh sách khách) giả định một domain chung đã tồn tại.

**Sửa cụ thể.** Thêm mục **"Câu hỏi mở"** dạng bảng: câu hỏi · vì sao nó chặn · **ai trả lời** · hạn. Kèm luật: câu hỏi mở chưa trả lời thì hạng mục phụ thuộc nó không được bắt đầu.

## H10 — Chiến lược test chỉ có mỗi AD-15; bài test quan trọng nhất theo ADR-002 không có mặt

**Sai ở đâu.** Toàn bộ chiều test = AD-15 (test kiến trúc) + thư mục `tests/Staxi.ArchitectureTests/`.

**Vì sao hỏng.** Thiếu, theo thứ tự nghiêm trọng:
1. **Bài test rò rỉ hai-tenant cho SignalR** — ADR-002 gọi nó là "**một trong những test đầu tiên của dự án, không phải test cuối**": dựng hai tenant giả, phát tin ở A, khẳng định B không nhận được gì. Nó là thứ duy nhất chứng minh AD-7 hoạt động (test kiến trúc chỉ chứng minh không ai gọi `Clients.All`, không chứng minh group được đặt tên đúng). Spine đánh rơi.
2. Không có **test tích hợp hai chiều với web cũ** — trong khi ADR-003/004 yêu cầu nó cho mọi bảng có hai người ghi.
3. Không nêu **framework test** (xUnit? NUnit? Vitest? Playwright?), không nêu test cho FE, không có **contract test giữa BFF và API** (AD-12 tạo ra một biên hợp đồng mới mà không có gì canh nó).
4. Không có ngưỡng chất lượng nào trong CI ngoài các test kiến trúc.

**Sửa cụ thể.** Thêm một AD "Chiến lược kiểm thử" ở mức spine: nêu 4 tầng (kiến trúc / đơn vị / tích hợp có DB thật / hợp đồng), ghim framework, và liệt kê **các bài test bắt buộc phải tồn tại trước khi hãng thí điểm lên**: rò rỉ tenant SignalR, rò rỉ tenant cache, rò rỉ tenant repository, `select-company` không leo thang quyền, lỗi trả về không phải HTTP 200.

## H11 — SQL Server không ghi phiên bản, cộng với `Microsoft.Data.SqlClient 7.0.3` mặc định `Encrypt=true`

**Sai ở đâu.** Bảng Stack: `SQL Server | giữ nguyên bản đang chạy`.

**Vì sao hỏng.** Từ **phiên bản 4.0**, `Microsoft.Data.SqlClient` đổi mặc định `Encrypt` thành **`true`**, và khi `TrustServerCertificate=false` thì tên server trong connection string **phải khớp chính xác** với tên trong chứng chỉ TLS của SQL Server. Hệ đang chạy kết nối bằng **IP nội bộ** (`<may-chu-noi-bo>` trong ADR-003), gần như chắc chắn dùng self-signed cert. Kết quả: ngày cắm `ITenantConnectionFactory` vào DB thật, **toàn bộ 18 kết nối fail ở bước handshake** với một thông báo về chuỗi chứng chỉ, và đội sẽ "sửa" bằng cách dán `TrustServerCertificate=True` vào 18 connection string đã mã hoá trong sổ đăng ký — một quyết định bảo mật bị lấy bởi người đang vội, lúc 11 giờ đêm.

Chưa kể: bản SQL Server thực tế quyết định cú pháp dùng được (`STRING_AGG`, `OPENJSON`, `GENERATE_SERIES`…) cho 2.003 proc sắp viết lại, và bản quá cũ có thể không thương lượng nổi TLS 1.2 với driver mới.

**Sửa cụ thể.** Ghi **phiên bản SQL Server thấp nhất trong 18 hãng** vào bảng Stack (một truy vấn `SELECT @@VERSION` chỉ-đọc là ra). Chốt chính sách mã hoá kết nối thành một dòng trong AD-14: `Encrypt=Mandatory` + cert nội bộ hợp lệ, hay `TrustServerCertificate=True` có ý thức — chọn một, viết ra, và để đó là quyết định của kiến trúc chứ không phải của sự cố.

## H12 — Luật "một bảng, một người ghi" bị đánh rơi

**Sai ở đâu.** ADR-003 và ADR-004 đều có: "⚠️ Một bảng, **một người ghi**. Nếu buộc phải hai, viết hợp đồng tường minh + test tích hợp hai chiều." AD-10 chỉ giữ phần DDL; AD-11 chỉ giữ phần last-write-wins.

**Vì sao hỏng.** Đây là luật duy nhất **giới hạn được** thiệt hại của AD-11. AD-11 chấp nhận LWW như một sự thật của vũ trụ, trong khi phần lớn bảng hoàn toàn có thể giữ được nguyên tắc một người ghi nếu có ai đó quyết định. Không có luật này, LWW áp cho **toàn bộ 565 bảng** thay vì cho một danh sách nhỏ được liệt kê và canh chừng.

**Sửa cụ thể.** Thêm vào AD-10 hoặc AD-11: mỗi bảng WEB2 ghi vào phải khai báo chủ sở hữu ghi (WEB2 / web cũ / cả hai). Danh sách "cả hai" là một danh sách **hữu hạn, có tên trong repo**, mỗi mục kèm lý do và một test tích hợp hai chiều. Ghi vào một bảng ngoài danh sách mà web cũ cũng ghi = vi phạm.

## H13 — CI/CD và phát hành FE tĩnh: chỉ có chữ "Jenkins"

**Sai ở đâu.** AD-1 bắt FE build ra file tĩnh đẩy lên CDN. Structural Seed nói "deploy bằng Jenkins". Hết.

**Vì sao hỏng.** Phát hành một SPA lên CDN có đúng một cái bẫy, và nó luôn nổ: **`index.html` phải không được cache còn asset có hash thì cache vĩnh viễn**. Làm ngược lại thì người dùng nhận `index.html` cũ trỏ tới asset đã bị xoá → màn hình trắng, và trên một hệ điều vận taxi thì đó là sự cố vận hành. Repo này **đã có tiền sử đúng loại đau này** (`CDN-UPLOAD-LIST.md`: quên upload thì mọi css/js 404). Spine không nói: ai đẩy file lên CDN, đẩy bằng gì, chiến lược hash/cache-control, có giữ bản cũ để rollback không, **phiên bản Node dùng để build** (AD-1 cấm Node ở runtime nhưng build thì bắt buộc cần), và làm sao FE biết địa chỉ gateway (build-time env hay runtime config — với "một bản build dùng chung cho mọi hãng" thì phải là runtime).

**Sửa cụ thể.** Một AD ngắn: `index.html` `no-store`; asset tên có hash, `Cache-Control: immutable, max-age=31536000`; giữ N bản build trước trên CDN; cấu hình runtime (địa chỉ gateway, tên hãng hiển thị) nạp từ một endpoint chứ không nhúng lúc build; ghim phiên bản Node trong Stack.

## H14 — CORS, và topo vẽ CDN nằm sau YARP

**Sai ở đâu.** Topo: `Y[YARP Gateway] --> CDN[React tĩnh - CDN]`.

**Vì sao hỏng.** Hai cách đọc, hệ quả khác hẳn nhau:
- Nếu trình duyệt tải FE **thẳng từ CDN** (đúng tinh thần AD-1) thì FE và API **khác origin** ⇒ **bắt buộc có chính sách CORS**, và preflight cho mọi request có `Authorization`. Spine không có một chữ nào về CORS. Nó cũng quyết định C4 (cookie `SameSite` và cookie cross-site cần `SameSite=None; Secure` + `credentials: include`).
- Nếu YARP **proxy lại** file tĩnh thì không còn là CDN nữa: mất cache biên, thêm một chặng, và gateway gánh lưu lượng tĩnh.

Mũi tên trong sơ đồ đang nói cách thứ hai, còn AD-1 nói cách thứ nhất.

**Sửa cụ thể.** Sửa sơ đồ (trình duyệt → CDN là một cạnh riêng, không qua YARP) và thêm một dòng CORS vào bảng quy ước: danh sách origin được phép, header được phép, có gửi credential không — một chỗ, cấu hình ở gateway, cấm từng dịch vụ tự khai.

## H15 — `StackExchange.Redis 3.2.1` không tồn tại; và dòng "đã kiểm chứng" không đúng với cả bảng

**Sai ở đâu.** Bảng Stack ghi `StackExchange.Redis | 3.2.1`, dưới tiêu đề "*Đã kiểm chứng trên NuGet và npm ngày 2026-09-17*".

**Kiểm chứng (17/09/2026).**

| Gói | Spine ghi | Thực tế |
|---|---|---|
| StackExchange.Redis | 3.2.1 | ❌ **Bản mới nhất là 3.1.31** (cập nhật 21/08/2026). Không có 3.2.x |
| Yarp.ReverseProxy | 2.3.0 | ✅ đúng là bản mới nhất |
| Dapper | 2.1.86 | ✅ |
| Microsoft.Data.SqlClient | 7.0.3 | ✅ |
| Serilog.AspNetCore | 10.0.0 | ✅ (28/11/2025) |
| Vite | 8.3.0 | ✅ nhánh 8.x có thật (8.0 ra 12/03/2026, Rolldown) |
| TypeScript | 7.0.2 | ✅ 7.0 GA 08/07/2026 — nhưng xem **M12** |
| .NET 10 hỗ trợ tới 14/11/2028 | ✅ | đúng |
| .NET 8 hết hỗ trợ 10/11/2026 | ✅ | đúng — lập luận của AD-1 vững |
| grate | *(không có)* | ❌ thiếu phiên bản |
| SQL Server | "giữ nguyên bản đang chạy" | ❌ thiếu phiên bản (xem H11) |

**Vì sao hỏng.** Một số phiên bản sai làm cả câu "đã kiểm chứng" mất giá trị — người đọc sau sẽ không biết dòng nào tin được. Và `restore` sẽ fail ngay lần đầu.

**Sửa cụ thể.** Sửa `StackExchange.Redis` về `3.1.31`, ghim grate, điền phiên bản SQL Server, và thêm cột **"vì sao chọn"** cho các mục không hiển nhiên (grate vs DbUp — ADR-003 đề xuất **DbUp** cấu hình ghi vào `[Admin.DbMigrations]`, spine chọn **grate** mà không giải thích vì sao đổi).

## H16 — AD-13 bắt dùng OpenTelemetry nhưng không có nơi nhận, không có ngưỡng giữ, và chồng vai với Serilog

**Sai ở đâu.** AD-13 chốt OTel + `traceparent` + lấy mẫu ("lệnh ghi và đổi phạm vi lưu 100%, phần còn lại lấy mẫu"). Stack có `Serilog.AspNetCore 10.0.0`.

**Vì sao hỏng.** Không nêu **collector/backend** (Jaeger? Tempo? Seq? Elastic? SQL như `[Admin.RequestTrace]` hiện nay?) — trên 6 máy on-prem, nơi nhận trace là một thành phần hạ tầng phải dựng, cấp phát ổ đĩa và vận hành, không phải một checkbox. Không nêu **tỉ lệ lấy mẫu** (chỉ nói "lấy mẫu"), không nêu **thời gian giữ**, không nêu ai được xem. Và quan hệ **Serilog ↔ OTel** không được định nghĩa: Serilog là sink xuất sang OTLP, hay hai đường log song song? Không chốt thì mỗi dịch vụ nối một kiểu, và `corrId` sẽ chỉ có mặt ở một trong hai đường.

**Sửa cụ thể.** Ghi rõ: backend nhận trace/metric/log + nơi chạy, `Serilog → OTLP` (hoặc bỏ Serilog, dùng `Microsoft.Extensions.Logging` + OTel exporter — một trong hai), tỉ lệ lấy mẫu bằng số, thời gian giữ, và `corrId` phải xuất hiện ở cả log lẫn trace lẫn header phản hồi (AD-8).

## H17 — Không có ngân sách hiệu năng, trong khi kiến trúc tự thêm ba chặng mạng

**Sai ở đâu.** Chiều hiệu năng chỉ xuất hiện ở hai câu định tính: AD-9 ("kiến trúc mới thêm một chặng mạng nên sai lầm này đắt gấp đôi") và AD-12 ("một màn hình nên là **một** lời gọi BFF").

**Vì sao hỏng.** Đường đi thật của một màn hình là: trình duyệt → HAProxy → YARP → BFF → API nghiệp vụ → SQL. Hệ cũ là: trình duyệt → IIS → SQL. Ta vừa thêm ba chặng cho một hệ điều vận realtime mà **không đặt một con số nào**: p95 cho một màn hình danh mục là bao nhiêu, cho Dashboard, cho độ trễ cập nhật bản đồ Online. ADR-002 đã hỏi thẳng "màn hình Online cần độ trễ bao nhiêu, bao nhiêu xe mỗi tenant lúc cao điểm?" và nói rõ câu trả lời **quyết định chọn SignalR hay SSE**. Spine chốt SignalR mà không có con số đó.

Không có ngân sách thì không ai biết lúc nào AD-12 ("một màn hình một lời gọi") bị vi phạm có hại, và không có tiêu chí để từ chối một thiết kế chậm.

**Sửa cụ thể.** Đặt ba con số vào spine: p95 cho lời gọi BFF của màn hình danh mục, p95 cho Dashboard, độ trễ tối đa của bản tin vị trí xe; kèm số xe/tenant lúc cao điểm. Ba con số này là điều kiện nghiệm thu, và chúng cũng giải quyết câu hỏi còn mở của ADR-002.

## H18 — Môi trường và dữ liệu thử nghiệm: bốn cái tên, không có nội dung

**Sai ở đâu.** "Môi trường: dev · devtest · staging · production."

**Vì sao hỏng.** Với kiến trúc 18 DB, câu hỏi bắt buộc là: **non-prod có bao nhiêu DB?** Một DB giả, hay bản sao của 18? Nếu là bản sao từ production thì dữ liệu chứa họ tên, số điện thoại, hành trình của lái xe và khách — **phải có luật che dữ liệu**, và đó là chiều "quyền riêng tư / lưu giữ dữ liệu" mà spine đang im lặng (bảng quy ước chỉ cấm **log** dữ liệu nhạy cảm, không nói gì về **bản sao DB**). Nếu là DB giả thì phải nói dữ liệu mẫu sinh ra từ đâu, vì một hệ có 565 bảng không tự có dữ liệu mẫu.

Thêm: sổ đăng ký hãng ở non-prod trỏ vào đâu? Một cấu hình sai ở staging trỏ vào DB production là một sự cố chỉ cần một dòng trong bảng — và AD-14 vừa làm cho việc thêm một dòng trở nên rất dễ.

**Sửa cụ thể.** Một bảng nhỏ: mỗi môi trường × (bao nhiêu tenant, dữ liệu từ đâu, che những cột nào, Redis riêng hay chung, sổ đăng ký riêng hay chung). Kèm luật: sổ đăng ký non-prod **không được chứa** connection string production, ép bằng kiểm tra lúc khởi động.

---

# MEDIUM

## M1 — Mâu thuẫn nội bộ: 350 bảng là "điều kiện tiên quyết" hay "hoãn"?

Mục *Database* viết: phân loại 350 bảng "là **điều kiện tiên quyết, không phải việc dọn dẹp**". Bảng *Deferred* viết: hoãn, xem lại "trước mỗi lần thêm một vùng nghiệp vụ mới". Hai câu này không thể cùng đúng, và AD-15 thì làm đỏ build khi "có thực thể chưa phân loại hạng tenant".

**Sửa.** Diễn đạt lại cho khớp: **phân loại theo thực thể, tại thời điểm chạm** — AD-15 ép mọi thực thể *được ánh xạ trong WEB2* phải có hạng; cái được hoãn là **cuộc kiểm kê trọn gói 350 bảng**, không phải nguyên tắc. Viết đúng như vậy thì cả ba chỗ hết mâu thuẫn.

## M2 — Danh pháp khoá: ba cách gọi cùng một thứ, và không có luật thoát ký tự

`{hãng}` (AD-6, AD-7), `tenantCode` (bảng Định danh), `t{TenantId}` (ADR-002). Khoá cache và tên group SignalR là **định dạng dây** dùng chung giữa nhiều tiến trình và cả backplane Redis — sai một chữ là hai dịch vụ không thấy nhau. Thêm nữa: dấu phân cách là `:`, nhưng không có luật gì cấm `{khoá}` chứa `:` → hai khoá khác nhau có thể sinh ra cùng một chuỗi.

**Sửa.** Một tên duy nhất (`tenantCode`), một văn phạm viết bằng chữ thật (`{env}:{tenantCode}:{companyId}:{region}:{key}`), luật charset cho `tenantCode` (`[a-z0-9-]{4,16}`, **bất biến sau khi cấp** — vì nó đã nằm trong mọi khoá cache và mọi tên group), và luật thoát/cấm `:` trong `{key}`.

## M3 — AD-9: đường biên proc ↔ .NET không có người gác và không có sổ đăng ký

ADR-003 cảnh báo đúng điểm này: "phải chốt tường minh và viết ra đường biên — nếu không, mỗi lập trình viên sẽ tự quyết theo từng màn hình, và sau một năm không ai nói được nghiệp vụ đang nằm ở đâu", và hỏi "**ai gác nó?**". AD-9 viết ra tiêu chí (đọc-ghi theo thực thể → .NET; gom dữ liệu → SQL) nhưng đó là tiêu chí **cần người phán xử**, và spine không chỉ định ai.

**Sửa.** Cơ học hoá: mỗi proc được giữ lại phải có một dòng trong `db/retained-procs.md` (tên proc · màn hình dùng · lý do giữ · người duyệt). Test CI quét mọi `CommandType.StoredProcedure` trong mã nguồn và làm đỏ nếu proc không có trong sổ. Vừa ép được AD-9, vừa trả lời dần câu hỏi "trong 2.003 proc, bao nhiêu cái còn được gọi".

## M4 — Hợp đồng danh sách (phân trang / sắp xếp / lọc): im lặng

Đây là một admin có **1243 view**, phần lớn là bảng dữ liệu có tìm kiếm. Không chốt hình dạng của request danh sách và của response phân trang thì mỗi màn hình sẽ có một kiểu (`page/size` vs `offset/limit` vs cursor; `total` có hay không; sắp xếp nhiều cột thế nào), và FE sẽ không tái dùng được một component bảng nào. Đây là điểm phân kỳ tầng dưới rõ nhất còn sót lại sau các mục trên.

**Sửa.** Một dòng trong bảng quy ước: khuôn request (`page`, `pageSize` trần bao nhiêu, `sort=field:asc`, filter), khuôn response (`items`, `total`, `page`), và hành vi khi vượt trần.

## M5 — Xuất Excel và tải tệp: im lặng

`AGENTS.md` nói rõ `BaseController` (3890 dòng) chứa **xuất Excel** như một năng lực dùng chung — nghĩa là gần như mọi màn hình báo cáo đều xuất Excel. WEB2 không có một chữ về nó: thư viện nào (ClosedXML/EPPlus — **EPPlus đã đổi sang giấy phép thương mại**), chạy ở BFF hay API, xuất đồng bộ hay nền, giới hạn số dòng, streaming hay dựng trong RAM (đặc biệt nguy hiểm khi AD-9 vừa cảnh báo chuyện kéo hàng triệu dòng lên tầng API).

**Sửa.** Một dòng quyết định + một dòng trong Stack.

## M6 — Kiểm tra dữ liệu vào (validation): không chốt

DataAnnotations hay FluentValidation, lỗi validation ánh xạ sang ProblemDetails ra sao (`errors` theo RFC 9457 mở rộng), thông báo tiếng Việt lấy từ đâu. Không chốt ⇒ hai kiểu, và FE phải xử lý hai hình dạng lỗi trong khi AD-8 vừa mới thống nhất được hình dạng lỗi.

## M7 — i18n phía FE: không chốt

Bảng quy ước chỉ nói "chuỗi giao diện và bình luận tiếng Việt". Web cũ dùng `App_GlobalResources/STaxiResource` (.resx). React cần một cơ chế (i18next / file JSON / hằng trong mã). Không chốt ⇒ ba màn hình đầu tiên sẽ có ba cách, và việc gom chuỗi về sau rất đắt. Cũng cần nói: có dự tính đa ngôn ngữ không, hay tiếng Việt cứng.

## M8 — Thư viện UI và bảng dữ liệu phía FE: không chốt (kèm rủi ro giấy phép)

Stack FE chỉ có React/Vite/TS/react-query/react-router — không có thư viện component, không có bảng dữ liệu, không có form. Với một admin đầy bảng, **thư viện bảng là quyết định kiến trúc**, không phải lựa chọn của người viết màn hình đầu tiên. Repo hiện dùng **AgGrid**, có phiên bản Enterprise tính phí — nếu WEB2 lặp lại lựa chọn đó thì đó là một cam kết ngân sách cần được nêu ở altitude này, chứ không phải phát hiện ra sau.

**Sửa.** Chốt hoặc ghi thành câu hỏi mở có người phụ trách: thư viện component, thư viện bảng (+ giấy phép), thư viện form.

## M9 — Lỗ hổng `ChangeCompany` bị đặt nhầm chỗ, không có người, không có hạn

Nó nằm trong bảng **"Rủi ro đã chấp nhận có ý thức"**. Một lỗ hổng leo thang quyền đang mở trên production **không phải một rủi ro được chấp nhận** — nó là một việc phải làm gấp, và cột "Quyết định" ghi "Không phải việc của WEB2" là cách chắc chắn nhất để nó không bao giờ được làm.

**Sửa.** Chuyển sang mục Câu hỏi mở / Việc phải làm, có **tên người** và **hạn**, và ghi rõ hai phần cần vá (thiếu kiểm quyền, và `GetRoleLastest` mất bộ lọc `FK_CompanyID`). D6 đã cho phép sửa web cũ thoải mái — không có lý do gì để trì hoãn.

## M10 — AD-8 không sống sót qua tầng hạ tầng

"Cấm trả HTTP 200 cho một lỗi" đúng cho mã ứng dụng, nhưng **IIS/ANCM vẫn trả trang HTML** cho 500.30/502.5, HAProxy trả trang HTML khi backend chết, và YARP trả lỗi của riêng nó. FE sẽ gặp HTML ở đúng những lúc tệ nhất.

**Sửa.** Thêm vào AD-8 mặt còn lại của hợp đồng: **FE coi mọi phản hồi không phải `application/problem+json`/`application/json` là lỗi**, bất kể mã trạng thái (đây chính là bài học đã ghi của hệ cũ, chỉ đổi hướng). Kèm cấu hình IIS/ANCM để không chèn trang lỗi riêng, và trang lỗi của HAProxy trả JSON.

## M11 — Spine nâng .NET 8 → .NET 10 nhưng không ghi nhận mình đang thay thế một quyết định đã chốt

`brainstorm-intent.md` chốt **D1 = .NET 8**, và cả bốn ADR đều viết trên nền D1. AD-1 đổi sang .NET 10 với một lý do đúng và đủ mạnh (8 hết hỗ trợ 10/11/2026) — nhưng không nói mình đang **thay thế D1**, nên bốn ADR nguồn giờ nói một đằng, spine nói một nẻo, và người đọc ADR trước sẽ hiểu sai.

**Sửa.** Một dòng trong AD-1: "Thay thế D1 (.NET 8) — lý do: …", và đánh dấu bốn ADR là đã được cập nhật theo spine (hoặc sửa dòng ".NET 8" trong chúng).

## M12 — TypeScript 7.0: API lập trình chưa ổn định cho tới 7.1

TS 7.0 GA 08/07/2026 với trình biên dịch Go (nhanh 8–12×) — nhưng **API lập trình ổn định phải đợi 7.1**, và các công cụ phụ thuộc API đó (typescript-eslint, template type-checking của một số framework) chưa chuyển sang được. Với một nền tảng mới dựng, "không có lint chuẩn" trong vài tháng đầu là một cái giá thật, và nó rơi đúng vào giai đoạn cần kỷ luật nhất.

**Sửa.** Giữ TS 7 nhưng ghi rõ trong Stack: trạng thái tooling, công cụ lint dùng tạm là gì, và điều kiện nâng lên 7.1. Hoặc bắt đầu bằng 6.x rồi nâng — nêu tường minh dù chọn đường nào.

## M13 — Rate limit chỉ tồn tại ở màn đăng nhập

AD-3 nêu rate limit cho login (rất đúng, kể cả cho mã hãng không tồn tại). Không có gì cho phần còn lại: một token hợp lệ có thể gọi một endpoint báo cáo nặng liên tục và kéo sập DB của hãng đó — trên kiến trúc **một deployment dùng chung**, một hãng có thể làm chậm cả 17 hãng còn lại (điều mà mô hình 18 deployment hôm nay ngăn được một cách tự nhiên). Đây là một rủi ro **mới sinh ra bởi D2** và không được nhắc.

**Sửa.** Một dòng trong AD-8 hoặc một AD riêng: quota theo (tenant, user) ở gateway, và giới hạn đồng thời cho các endpoint báo cáo; nêu rõ đây là bù cho việc mất cách ly tài nguyên giữa các hãng.

## M14 — Health check / readiness / cảnh báo chỉ có cho sổ đăng ký

AD-14 yêu cầu health check riêng cho sổ đăng ký — tốt. Nhưng không có gì cho phần còn lại: HAProxy kiểm cái gì để coi một instance là sống, readiness khác liveness thế nào (một instance chưa nạp xong cache sổ đăng ký thì chưa được nhận lưu lượng), ai nhận cảnh báo, ngưỡng nào thì báo. Trên 6 máy tự vận hành, đây là phần không thể vay mượn từ cloud.

---

# LOW

## L1 — Frontmatter rỗng và trạng thái không có người duyệt

`binds: []` và `companions: []` để trống trong khi spine rõ ràng bind tới 4 ADR và tới brainstorm; `status: draft` nhưng không ghi ai duyệt và duyệt thì thành gì. Nhỏ, nhưng làm việc truy vết về sau khó hơn mức cần thiết.

## L2 — Bảng "Rủi ro đã chấp nhận" trộn ba loại khác nhau

Trong bốn dòng có: hai rủi ro thật sự được chấp nhận (LWW, Domain không ép được bất biến), một đánh đổi có bù đắp (SPOF sổ đăng ký), và **một việc phải làm gấp** (`ChangeCompany`, xem M9). Trộn lẫn làm người đọc lướt qua cả bảng với tâm thế "đã xử lý rồi".

## L3 — Sơ đồ topo không nói rõ HAProxy hiện có đứng trước 18 site cũ không

Cạnh `HA -.domain cũ, chưa chuyển.-> OLD` gợi ý HAProxy phục vụ cả web cũ, trong khi AD-1/paradigm khẳng định "web cũ **không bị đụng tới**". Nếu WEB2 thật sự thêm tải vào một HAProxy đang phục vụ production của 18 hãng, thì dòng Deferred "HAProxy đang là điểm chết đơn lẻ — rủi ro đã tồn tại từ trước, **không phải do WEB2 tạo ra**" là một lập luận yếu hơn nó tưởng: WEB2 không tạo ra SPOF, nhưng WEB2 **làm tăng tải và tăng bán kính** lên nó ngay từ hãng đầu tiên.

---

# Những chỗ spine làm đúng (giữ nguyên, đừng sửa khi làm lại vòng sau)

- **AD-3 — phân giải tenant trước khi xác thực.** Đây là nước đi hay nhất trong cả tài liệu: nó xoá sổ ba rủi ro (con gà–quả trứng, identity store trung tâm, trùng username giữa 18 DB) bằng một thay đổi ở tầng UX. Kèm luật "mã sai và mật khẩu sai trả cùng một thông báo" + "rate limit tính cả mã không tồn tại" — hai luật ép được và đóng đúng lỗ dò danh sách khách hàng.
- **AD-4 + AD-7.** Chốt đúng hai khe rò rỉ nguy hiểm nhất, và AD-7 phản ánh chính xác phát hiện kỹ thuật khó nhất của ADR-002 (tenant context **không** sống qua các lần gọi hub method — mỗi lần gọi là một DI scope mới).
- **AD-10.** Luật schema tương thích ngược là thứ giữ cho toàn bộ giai đoạn song song không nổ, và nó được viết ở dạng danh sách được/cấm, đọc là làm được.
- **AD-11.** Trung thực hiếm gặp: ghi "*(có ý thức không ngăn)*" ở ô Prevents thay vì giả vờ đã giải quyết. Câu "bất biến nghiệp vụ sống còn phải nằm **trong database**" là kết luận đúng và ít người chịu viết ra.
- **Ý tưởng AD-15.** Đúng tinh thần "biến lỗi production thành lỗi build". Vấn đề chỉ là danh sách còn thiếu (H3), không phải cách tiếp cận.
- **Phê chuẩn brownfield.** Giữ `I{X}Service`/`I{X}Repository`, `_camelCase`, tiếng Việt cho chuỗi giao diện và bình luận; giữ `companyId` kiểu `int` kế thừa; giữ id bản ghi nguyên kiểu; giữ SQL Server và Dapper + proc thay vì ép ORM lên 2.003 proc. Đây là phê chuẩn đúng chỗ và tiết kiệm rất nhiều tranh cãi về sau.
- **Bỏ ép culture toàn tiến trình** là sửa đúng một khuyết tật thật của web cũ — chỉ cần tách khỏi chuyện *lưu trữ* (C1).

---

# Việc cần làm trước khi review lại

**Chặn cổng (phải xong):** C1 · C2 · C3 · C4 · C5 · C6 · C7 · H1 · H2 · H3 · H5 · H6 · H9.

**Nên xong cùng vòng đó:** H4 · H7 · H8 · H10 · H11 · H12 · H13 · H14 · H15 · H16 · H17 · H18.

**Cấu trúc cần thêm vào tài liệu:** một mục **"Câu hỏi mở"** (có người, có hạn) và một mục **"Envelope vận hành"**. Thiếu hai mục này thì vòng sau vẫn sẽ hở đúng chỗ cũ.

---

## Nguồn kiểm chứng phiên bản

- [NuGet — StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis/)
- [NuGet — Yarp.ReverseProxy](https://www.nuget.org/packages/Yarp.ReverseProxy)
- [NuGet — Dapper](https://www.nuget.org/packages/Dapper/)
- [NuGet — Microsoft.Data.SqlClient](https://www.nuget.org/packages/microsoft.data.sqlclient)
- [NuGet — Serilog.AspNetCore](https://www.nuget.org/packages/serilog.aspnetcore)
- [SqlConnectionStringBuilder.Encrypt — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlconnectionstringbuilder.encrypt?view=sqlclient-dotnet-standard-5.2)
- [Announcing TypeScript 7.0 — Microsoft DevBlogs](https://devblogs.microsoft.com/typescript/announcing-typescript-7-0/)
- [Vite 8.0 is out! — vite.dev](https://vite.dev/blog/announcing-vite8)
- [grate — Folder configuration](https://github.com/grate-devs/grate/blob/main/docs/ConfigurationOptions/FolderConfiguration.md)
