---
review: security
lens: 'Cách ly multi-tenant — tìm đường dữ liệu hãng A chạm tới hãng B'
target: ARCHITECTURE-SPINE.md (WEB2)
inputs: ADR-002, ADR-004
reviewer: security
date: '2026-09-17'
verdict: 'ĐỔI HƯỚNG — hướng đi đúng, nhưng phần lớn luật cách ly đang là lời khuyên chứ chưa phải cơ chế'
---

# Rà soát an ninh — ARCHITECTURE-SPINE (WEB2)

## Kết luận một dòng

Bộ khung này **nghĩ đúng về mô hình đe doạ** — nó gọi tên đúng lỗ hổng `ChangeCompany` của web cũ, đúng rủi ro `Clients.All`, đúng chuyện lộ danh sách khách hàng — nhưng **phần lớn các điều "cấm" không có cơ chế ép**, và **ba trụ cột cách ly (connection factory, cache dùng chung, ghi vết) đều có một khe hở cấu trúc** khiến một lần sai phạm vi tenant vừa xảy ra được, vừa không phát hiện được sau đó. Không nên cho hãng đầu tiên lên production trước khi đóng nhóm CRITICAL.

## Cách đọc bảng điểm

| Mức | Nghĩa trong bối cảnh này |
|---|---|
| **CRITICAL** | Có đường đi khả dĩ để dữ liệu hãng A tới hãng B, hoặc mất khả năng phát hiện việc đó |
| **HIGH** | Chưa rò trực tiếp nhưng hạ đáng kể chi phí tấn công, hoặc là một khoảng trắng bắt buộc phải có |
| **MEDIUM** | Rủi ro thật nhưng cần thêm điều kiện, hoặc chủ yếu ảnh hưởng tính sẵn sàng |
| **LOW** | Nên sửa, chi phí thấp, không chặn phát hành |

---

# Phần A — Luồng đăng nhập và phân giải tenant

## S-01 · HIGH · Thông báo đồng nhất nhưng **thời gian phản hồi thì không** — vẫn dò được mã hãng

**Điểm yếu.** AD-3 và ADR-004 chốt "mã hãng sai và mật khẩu sai trả về cùng một thông báo". Nhưng hai trường hợp đó đi qua **hai khối lượng công việc khác hẳn nhau**: mã hãng sai dừng ngay ở sổ đăng ký (một lần tra cache cục bộ, vài chục micro giây); mã hãng đúng thì phải giải mã connection string, mở kết nối tới DB hãng đó, truy vấn bảng user, rồi **băm mật khẩu** (một hàm cố tình chậm, nếu làm đúng). Chênh lệch này lớn tới mức đo được qua Internet. Thông báo giống nhau mà thời gian khác nhau thì **kênh dò vẫn còn nguyên**, chỉ đổi từ đọc chữ sang bấm giờ.

Cùng lớp yếu điểm, các kênh phụ khác chưa được kể tới: độ dài phản hồi, số lượng header, việc có/không sinh `corrId`, mã HTTP (401 và 400 khác nhau là lộ), thứ tự bật rate limit (nếu chỉ bật limit cho mã hãng tồn tại thì việc bị/không bị chặn chính là câu trả lời), và **thời gian của lần đăng nhập đầu tiên vào một hãng lâu không dùng** (cold pool) so với hãng đang hoạt động.

**Cách đóng.** Thêm **AD-19 — chống liệt kê hãng bằng công-sức-hằng-số**: đường xử lý đăng nhập luôn thực hiện **đúng một chuỗi công việc** bất kể mã hãng có tồn tại hay không — mã hãng không tồn tại vẫn chạy một lần băm mật khẩu giả với một hash cố định cấu hình sẵn; phản hồi thất bại là **một đối tượng ProblemDetails duy nhất, cùng mã HTTP, cùng tập header**; thêm sàn thời gian phản hồi (ví dụ làm tròn lên 300 ms ± nhiễu ngẫu nhiên) áp cho mọi nhánh thất bại. Khoá lại bằng một bài test đo phân phối thời gian của ba nhánh (mã sai / user sai / mật khẩu sai) và làm đỏ build khi lệch quá ngưỡng.

## S-02 · HIGH · "Cấm endpoint liệt kê hãng" chỉ chặn **một** endpoint, không chặn **lớp** endpoint

**Điểm yếu.** AD-3 cấm "mọi endpoint liệt kê hãng khi chưa xác thực" và ADR-004 cấm `GET /tenants`. Nhưng bề mặt trước-đăng-nhập rộng hơn thế, và ADR-004 lại tự mở một cửa: "Thương hiệu/màu/logo từng hãng là **dữ liệu từ API config**". Nếu React gọi một API lấy logo/màu **sau khi người dùng gõ mã hãng và trước khi đăng nhập** (cách làm tự nhiên nhất để màn login có thương hiệu), thì đó **chính là một endpoint kiểm tra sự tồn tại của mã hãng** — không liệt kê hàng loạt, nhưng dò từng mã một thì cũng ra cùng kết quả. Tương tự: một endpoint "quên mã hãng", một health check trả tên hãng, thông báo lỗi có `corrId` khác khuôn, hay YARP trả 503 khác 404 cho tenant chưa cấu hình.

**Cách đóng.** Viết luật theo **lớp** chứ không theo tên endpoint: *"Không tồn tại phản hồi chưa xác thực nào mà nội dung, thời gian hay mã trạng thái phụ thuộc vào việc một mã hãng có tồn tại hay không."* Cụ thể: thương hiệu chỉ tải **sau khi có token đầy đủ** (màn login dùng giao diện trung tính); mọi route chưa xác thực nằm trong một danh sách trắng khai báo tường minh ở gateway, và **AD-15 thêm một bài test duyệt bảng route**, làm đỏ build khi có route không đòi xác thực mà không nằm trong danh sách trắng đó (`FallbackPolicy = RequireAuthenticatedUser`).

## S-03 · HIGH · Rate limit được nhắc tên nhưng không có chiều, không có kho, không có chỗ đặt — và không chặn credential stuffing chéo mã hãng

**Điểm yếu.** AD-3 chỉ nói "rate limit tính cả trường hợp mã hãng không tồn tại". ADR-004 có chi tiết hơn (IP, username, `(hãng, username)`) nhưng **bộ khung đã đánh rơi phần đó**, mà bộ khung mới là thứ đội phát triển đọc. Ba hệ quả:

1. **Thiếu chiều "một credential trải qua nhiều mã hãng".** Bộ đếm theo `(hãng, username)` **không bao giờ chạm ngưỡng** khi kẻ tấn công lấy một cặp user/mật khẩu rò rỉ và thử lần lượt qua 18 mã hãng — mỗi hãng đúng một lần thử. Đây là dạng tấn công tự nhiên nhất với một cổng đăng nhập dùng chung cho 18 khách.
2. **Không nói kho đếm nằm ở đâu.** Nếu bộ đếm nằm trong tiến trình, 6 máy × N instance nghĩa là ngưỡng thực tế **nhân lên theo số instance**, và nó reset mỗi lần deploy.
3. **Không nói đặt ở tầng nào.** Đặt sau gateway thì mỗi dịch vụ tự làm một kiểu; đặt ở HAProxy thì mất chiều username.

**Cách đóng.** Trong AD-19: rate limit **tập trung, trạng thái trên Redis**, áp ở **gateway**, theo bốn chiều đồng thời — `IP`, `(hãng, username)`, `username` **xuyên mọi hãng**, và `hash(username+password)` **xuyên mọi hãng** (chiều cuối là chiều chặn credential stuffing). Thêm một tín hiệu phát hiện riêng: *"một nguồn tạo thất bại đăng nhập trên ≥ 3 mã hãng khác nhau trong 10 phút"* → cảnh báo, vì hành vi đó không có kịch bản hợp lệ nào. Kèm khoá tài khoản tăng dần (exponential backoff) và CAPTCHA/PoW sau ngưỡng.

## S-04 · HIGH · Không nói gì về **cách lưu mật khẩu kế thừa**, khoá tài khoản, hay xác thực hai yếu tố

**Điểm yếu.** AD-3 nói "xác thực trên bảng user của chính DB hãng đó" — tức là **dùng lại cơ chế băm mật khẩu của hệ cũ**, mà bộ khung không hề nói cơ chế đó là gì. Với một codebase ASP.NET MVC 5 kế thừa, khả năng cao là MD5/SHA1 không salt hoặc salt tĩnh. WEB2 sẽ **kế thừa toàn bộ điểm yếu đó và hợp pháp hoá nó** bằng cách xây nền tảng mới lên trên. Đây là admin console của 18 hãng taxi: một tài khoản admin bị lấy là toàn bộ dữ liệu một hãng.

Cũng vắng mặt: chính sách mật khẩu, khoá tài khoản sau N lần sai, MFA cho vai trò quản trị, và quy trình đổi mật khẩu/đặt lại mật khẩu (đường đặt lại mật khẩu là một bề mặt liệt kê tenant nữa).

**Cách đóng.** Một AD riêng về danh tính kế thừa: (a) **khảo sát và ghi lại** thuật toán băm hiện tại trước khi viết dòng code auth đầu tiên; (b) nếu yếu thì **nâng cấp tại chỗ lúc đăng nhập** — xác thực bằng hash cũ, rồi ghi đè bằng hash mới (PBKDF2/Argon2id) vào một cột `NULL`-able mới, đúng luật tương thích ngược AD-10, và web cũ không cần biết; (c) **MFA bắt buộc cho vai trò quản trị hãng** trước khi hãng thứ ba lên; (d) đặt lại mật khẩu chỉ qua kênh đã xác thực, không tự phục vụ công khai.

## S-05 · MEDIUM · `tenantCode` là một định danh **an ninh** nhưng chưa có dạng chuẩn hoá và bộ ký tự đóng

**Điểm yếu.** `tenantCode` được dùng lại ở bốn nơi có ý nghĩa an ninh: khoá tra sổ đăng ký, **thành phần khoá cache** (AD-6), **thành phần tên group SignalR** (AD-7), và **tiền tố backplane Redis** (AD-7). Bộ khung chỉ nói nó "là chuỗi, không đoán được theo quy luật". Không có bộ ký tự, không có dạng chuẩn hoá. Ba hệ quả cụ thể:

- **Chèn dấu phân cách.** Khuôn khoá là `{hãng}:{companyId}:{vùng}:{khoá}`. Một mã hãng chứa `:` làm nhoè ranh giới các tầng — hai giá trị khác nhau ánh xạ về cùng một chuỗi khoá, tức hai hãng dùng chung một ô cache.
- **Chuẩn hoá không nhất quán.** Nếu tra sổ đăng ký không phân biệt hoa/thường (mặc định của collation SQL Server tiếng Việt) mà dựng khoá cache thì có phân biệt, `HangA` và `hanga` là **một tenant với hai không gian cache** — bất nhất, và ngược lại nếu lệch chiều thì là hai tenant chung một không gian. Cùng vấn đề với chữ Unicode đồng hình và khoảng trắng đầu/cuối.
- **Né rate limit.** Biến thể hoa/thường của cùng một mã tạo ra các khoá đếm khác nhau.

**Cách đóng.** Thêm **AD-16**: `tenantCode` khớp `^[a-z0-9][a-z0-9-]{5,31}$`, sinh ngẫu nhiên khi tạo hãng; **một hàm chuẩn hoá duy nhất** chạy ở biên vào (trim, lowercase invariant, kiểm regex, từ chối nếu sai) trước mọi việc dùng tiếp; kiểu `TenantCode` là một value object, không phải `string`, và không có đường dựng nó từ chuỗi thô ngoài hàm đó. AD-15 kiểm: không có `string` nào đi thẳng vào hàm dựng `CacheKey` hoặc hàm sinh tên group.

---

# Phần B — Token

## S-06 · CRITICAL · Luật "cấm nhận `tenantId`/`companyId` từ request" — **luật chủ lực của bộ khung — không có trong danh sách chốt chặn AD-15**

**Điểm yếu.** AD-4 tồn tại để không lặp lại `ChangeCompany`. Nó là luật quan trọng nhất trong cả tài liệu. Nhưng AD-15 liệt kê bảy bài test và **không có bài nào kiểm điều này**. Tức là luật chống lại đúng lỗ hổng đã biết của hệ thống đang chạy đang tồn tại dưới dạng **một câu trong tài liệu**. Nó sẽ bị vi phạm — không phải vì ai đó cố tình, mà vì một lập trình viên viết một endpoint báo cáo và thêm `companyId` vào DTO lọc, thấy nó chạy, và không ai cản.

Điều này đặc biệt dễ xảy ra với BFF (AD-12): BFF gom dữ liệu cho màn hình, và một màn hình báo cáo "so sánh các công ty" rất tự nhiên sẽ nhận một danh sách `companyId` từ client.

**Cách đóng.** Ba lớp, cả ba đều rẻ:

1. **Chặn ở model binder.** Một `IModelBinderProvider` toàn cục ném lỗi khi bất kỳ tham số route/query/body/header/form nào có tên khớp `(?i)tenant|company|hang|xncode`. Một danh sách ngoại lệ khai báo tường minh kèm lý do, y hệt cách AD-15 xử lý static state.
2. **Test kiến trúc.** Quét mọi kiểu DTO đầu vào và mọi tham số action; đỏ build khi gặp tên khớp mà không có trong danh sách ngoại lệ. Bổ sung vào danh sách AD-15.
3. **Kiểm ở gateway.** YARP xoá mọi header đến từ client có tiền tố `X-Tenant`/`X-Company` trước khi chuyển tiếp — để một header nội bộ không bao giờ bị giả từ ngoài.

## S-07 · CRITICAL · Chữ ký token: không nói thuật toán, khoá, xoay khoá, hay ai xác thực nó — bán kính là cả 18 hãng

**Điểm yếu.** Bộ khung nói token "mang `tenant` + `companyId` + `permissionVersion`" và "JWT stateless, mang `SecurityStamp`". Không nói: thuật toán ký, khoá ở đâu, ai giữ, xoay bao lâu một lần, có `kid` không, `iss`/`aud`/`exp` có được kiểm không, danh sách thuật toán chấp nhận có đóng không.

Với multi-tenant thì hệ quả rất cụ thể: **một khoá ký duy nhất bảo vệ ranh giới giữa 18 hãng**. Claim `tenant` chỉ đáng tin bằng đúng khoá đó. Các lớp yếu điểm đi kèm:

- **Khoá đối xứng dùng chung.** Nếu ký bằng HMAC và mọi dịch vụ (gateway, BFF, API, notification) đều giữ khoá để *xác thực*, thì mọi dịch vụ cũng **đủ sức tạo token** cho bất kỳ tenant nào. Một lỗ RCE/đọc-file ở dịch vụ ít quan trọng nhất trở thành quyền truy cập toàn hệ.
- **Khoá dùng chung giữa môi trường.** Nếu dev/devtest/staging dùng cùng khoá với production, một token lấy từ máy dev mở được production. Bộ khung liệt kê 4 môi trường và không nói gì về tách khoá.
- **Danh sách thuật toán mở.** Không nói "chỉ chấp nhận đúng một `alg`" là để ngỏ nhóm lỗi nhầm lẫn thuật toán.
- **Dịch vụ tin gateway.** Sơ đồ topo cho thấy mọi thứ đi qua YARP. Nếu các dịch vụ phía sau **không tự xác thực token** mà tin vào một header do gateway gắn, thì bất kỳ đường nào chạm thẳng tới cổng dịch vụ (SSRF từ chính hệ, một máy trong cùng mạng, một quy tắc firewall sai) là bỏ qua toàn bộ cách ly. Bộ khung không nói các dịch vụ tự kiểm.

**Cách đóng.** Thêm **AD-18 — vòng đời token**: ký **bất đối xứng** (ES256/RS256), khoá riêng **chỉ nằm ở Auth API**, các dịch vụ khác chỉ có khoá công khai qua JWKS; `kid` bắt buộc, xoay khoá định kỳ có chồng lấp; danh sách `alg` đóng đúng một giá trị; kiểm bắt buộc `iss`/`aud`/`exp`/`nbf`; **khoá tách hoàn toàn theo môi trường**, và `aud` mang tên môi trường để một token staging bị từ chối ở production ngay cả khi khoá lỡ trùng; **mọi dịch vụ tự xác thực token**, gateway là tiện ích chứ không phải ranh giới tin cậy; kênh nội bộ HAProxy → YARP → dịch vụ dùng TLS (bearer token đang đi qua 6 máy).

## S-08 · HIGH · Đổi công ty cấp token mới nhưng **không nói token cũ chết**

**Điểm yếu.** AD-4: "Đổi công ty = gọi `select-company`, kiểm tra lại quyền, **cấp token mới**". Không có câu nào nói **token cũ hết hiệu lực**. Với JWT stateless, mặc định là nó **vẫn sống tới hạn**. Nghĩa là sau khi chuyển từ công ty A sang B, người dùng (hoặc bất kỳ ai cầm được token đó) vẫn giữ một token hợp lệ cho A. Nếu quyền ở A vừa bị thu hồi, việc thu hồi không có tác dụng gì cho tới khi token hết hạn. Đây cũng là đường lách cho kịch bản "hạ quyền": người dùng lưu lại token quyền cao trước khi bị hạ.

Cùng nhóm: bộ khung nói "kiểm lại `SecurityStamp` + `permissionVersion` mỗi request" — nhưng **không nói độ trễ thu hồi là bao nhiêu**, giá trị đối chiếu nằm ở đâu, và ai đọc nó. Nếu đọc từ DB hãng mỗi request thì đó là một round trip DB cho **mọi** request (mâu thuẫn với lý do chọn stateless, và làm sổ đăng ký + DB hãng thành nút thắt). Nếu cache giá trị đó thì **độ trễ thu hồi = TTL cache**, và con số đó chưa từng được đặt ra.

**Điểm nguy hiểm nhất, dạng vòng tròn:** AD-6 nói khoá cache "mang tem `permissionVersion` khi nội dung phụ thuộc quyền". Nếu giá trị `permissionVersion` dùng để *đối chiếu* cũng được cache theo khoá có chứa `permissionVersion` **lấy từ chính token**, thì token tự chứng minh mình còn hợp lệ — cache luôn trúng, và thu hồi không bao giờ có tác dụng.

**Cách đóng.** Trong AD-18: access token **TTL ngắn (≤ 10 phút)**, refresh token quay vòng (rotation) có phát hiện tái sử dụng; `select-company` **tăng `permissionVersion`** hoặc ghi `jti` của token cũ vào danh sách chặn tới hạn cũ; giá trị đối chiếu `permissionVersion`/`SecurityStamp` nằm ở **Redis, khoá theo `(hãng, userId)`**, ghi bởi Auth API khi có thay đổi, đọc bởi các dịch vụ — **khoá đó tuyệt đối không chứa `permissionVersion` lấy từ token**; nêu rõ một con số ngân sách: *"độ trễ thu hồi tối đa 60 giây, kể cả SignalR"*, và đó là thứ kiểm được bằng test.

## S-09 · HIGH · "Token tạm" ở màn chọn công ty chưa có phạm vi — nguy cơ fail-open trong nội bộ một hãng

**Điểm yếu.** Sơ đồ luồng đăng nhập cấp "token tạm + danh sách công ty" khi user thuộc nhiều công ty. Bộ khung không nói token tạm đó **được phép gọi gì**. Nó đã mang `tenant` nhưng chưa mang `companyId`. Câu hỏi quyết định: một endpoint nghiệp vụ nhận token thiếu `companyId` sẽ làm gì? Nếu repository diễn giải "không có companyId" thành "không lọc theo công ty" thì token tạm **đọc được dữ liệu của mọi công ty trong hãng** — đúng lỗi `ChangeCompany` của web cũ, chỉ đến từ hướng khác. Đây là biến thể "session fixation" đúng nghĩa của kiến trúc token: một artefact trung gian mang quyền chưa được định nghĩa.

**Cách đóng.** Token tạm có `typ`/`aud` **riêng biệt**, TTL ≤ 120 giây, và **duy nhất** endpoint `select-company` chấp nhận `aud` đó; mọi endpoint khác từ chối. Song song, quy tắc **fail closed** ở tầng dữ liệu: `TenantScope` thiếu `companyId` khi truy vấn một thực thể `ITenantOwned` là **ném lỗi**, không phải bỏ điều kiện lọc. Bổ sung vào AD-5 — đây là dòng luật thiếu quan trọng nhất của AD-5.

## S-10 · MEDIUM · Bearer token phát lại được, không có ràng buộc gì

**Điểm yếu.** Token là bearer thuần: ai cầm được thì dùng được, từ IP nào cũng được, tới khi hết hạn. Không có ràng buộc thiết bị, không có `jti` để truy vết một token cụ thể, và bộ khung không nói token nằm ở đâu trong trình duyệt (xem S-20).

**Cách đóng.** `jti` bắt buộc và ghi vào audit (để một token bị lạm dụng truy ngược được thành một chuỗi hành động); TTL ngắn như S-08; cân nhắc DPoP hoặc ràng buộc refresh token theo thiết bị cho vai trò quản trị. Không cần mTLS phía client, nhưng nên ghi rõ đã cân nhắc và vì sao bỏ.

---

# Phần C — Connection factory và tầng dữ liệu

## S-11 · CRITICAL · `ITenantConnectionFactory` là đường duy nhất, nhưng **đầu vào của nó chưa được khoá** — mỗi chỗ gọi vẫn là một chỗ quyết định

**Điểm yếu.** AD-2 nói factory là đường duy nhất và AD-15 kiểm `new SqlConnection`. Bài test đó chứng minh **factory được dùng** — nó **không** chứng minh factory được dùng **với đúng tenant**. Nếu chữ ký là `OpenAsync(tenantCode)` thì mọi nơi gọi đều có thể truyền sai giá trị, và kẻ tấn công không cần gì hơn một endpoint nào đó chuyền `tenantCode` từ đầu vào xuống (ghép với S-06 thì đây là một chuỗi hoàn chỉnh). Bài test kiến trúc vẫn xanh.

**Cách đóng.** Thêm **AD-17**: `ITenantConnectionFactory` **không nhận tham số tenant**. Nó đọc `TenantContext` — một kiểu **bất biến, sealed, không có setter công khai**, được đặt **đúng một lần** bởi một middleware xác thực duy nhất (adapter `HttpContext` duy nhất mà AD-15 đã cho phép), và **không có API nào trong tầng Application/Api đặt lại được nó**. Không có context ⇒ **ném lỗi**, không có giá trị mặc định, không có fallback "tenant hệ thống". AD-15 bổ sung: đỏ build khi `TenantContext` được gán ngoài assembly adapter.

## S-12 · CRITICAL · Một tài khoản SQL dùng chung cho 18 DB biến **một lỗi code thành một vụ rò dữ liệu**; tài khoản riêng biến nó thành một lỗi quyền

**Điểm yếu.** Bộ khung không nói gì về danh tính đăng nhập SQL. Mô hình mặc định (và rẻ nhất về vận hành) là **một login ứng dụng dùng chung**, có quyền trên cả 18 DB, chỉ khác `Initial Catalog`. Dưới mô hình đó, **chỉ có chuỗi kết nối** ngăn hãng A đọc hãng B — tức là ranh giới cách ly cuối cùng là một biến chuỗi trong bộ nhớ tiến trình. Bất kỳ lỗi nào ở S-11, S-13, hoặc một câu SQL có tên DB đủ điều kiện (`[HangB].[dbo].[Trip]`) là rò ngay lập tức, và **SQL Server không phản đối gì cả**.

Đây là điểm đòn bẩy lớn nhất trong toàn bộ bản rà soát này: nó không ngăn lỗi xảy ra, nhưng nó **đổi hậu quả của mọi lỗi khác** từ "dữ liệu chảy sang hãng khác, im lặng" thành "lỗi quyền, có log, có cảnh báo".

**Cách đóng.** Trong AD-17:
- **Mỗi hãng một login SQL riêng**, chỉ có quyền trên đúng DB của hãng đó, không phải `sysadmin`, không `db_owner`.
- **Cấm `ChangeDatabase()`, cấm câu lệnh `USE`, cấm tên đối tượng đủ ba/bốn phần** (cross-database, linked server) trong mọi câu SQL của WEB2 — thêm vào danh sách AD-15 (kiểm được bằng phân tích chuỗi SQL literal + một bài test tích hợp).
- Quyền ghi tách khỏi quyền đọc nếu khả thi; login dùng cho `.AcrossTenants` (S-15) là **một login khác**, có quyền khác, và mọi phiên của nó đều bị cảnh báo.
- Ưu tiên **xác thực không mật khẩu** (Windows/Managed Identity) — nó xoá luôn phần lớn S-18.

## S-13 · CRITICAL · Ngữ cảnh tenant kiểu ambient rò qua ranh giới bất đồng bộ và qua phụ thuộc bị "bắt giữ"

**Điểm yếu.** AD-15 cấm "trạng thái static thay đổi được" — tốt, nhưng nó **không bắt được** lớp lỗi nguy hiểm nhất của multi-tenant trong DI: **một singleton giữ một phụ thuộc scoped** (captive dependency). Một `IMemoryCache`-wrapper, một `IHostedService`, một background worker, hay một service đăng ký nhầm là singleton mà nhận `ITenantConnectionFactory` (scoped) sẽ **giữ mãi connection factory của request đầu tiên** — và phục vụ nó cho mọi request sau, thuộc mọi hãng. Không có `static` nào ở đây cả; bài test AD-15 hiện tại xanh.

Nhóm liên quan:
- **Công việc chạy nền tách khỏi request** (`Task.Run`, fire-and-forget, timer, hàng đợi in-process): `AsyncLocal` hoặc mất giá trị (rồi fallback về đâu?), hoặc **giữ lại giá trị của request đã kết thúc từ lâu**.
- **SignalR**: AD-7 đã xử đúng vấn đề scope-mới-mỗi-lần-gọi, nhưng chỉ cho SignalR. Cùng lớp vấn đề cũng đúng cho mọi công việc nền, và bộ khung không tổng quát hoá.
- **Object pool / kiểu tái dùng** giữ lại `TenantScope` cũ.

**Cách đóng.** Trong AD-17 và AD-15:
- Bật `ValidateOnBuild` **và** `ValidateScopes` ở **mọi** môi trường kể cả production; một bài test dựng toàn bộ container và **đỏ build** khi có singleton phụ thuộc scoped.
- Công việc chạy nền **không có ngữ cảnh ambient**: phải gói trong `ITenantScopeRunner.RunAsAsync(tenantScope, work)` tạo scope DI mới và đặt context tường minh. AD-15 kiểm: đỏ build khi gặp `Task.Run`/`_ = SomeAsync()` trong tầng Application/Infrastructure ngoài runner đó.
- Quy tắc mặc định của cả hệ: **không có ngữ cảnh ⇒ ném lỗi**. Không bao giờ có "tenant mặc định".

## S-14 · MEDIUM · Pool kết nối: an toàn theo chuỗi, nhưng là một đường DoS chéo hãng — và bộ khung đã đánh rơi con số của ADR-004

**Điểm yếu.** ADO.NET phân pool theo chuỗi kết nối, nên pool **tự nó** không trộn tenant (miễn là S-12 được thực hiện và không ai gọi `ChangeDatabase`). Nhưng ADR-004 đã tính `18 × 6 × 100 = 10.800` kết nối tiềm năng về phía SQL và cảnh báo phải đặt có chủ đích — **bộ khung không nhắc lại con số này ở đâu cả**. Hệ quả thực tế: một hãng chạy báo cáo nặng làm cạn thread pool/kết nối của tiến trình dùng chung, và **17 hãng còn lại chậm hoặc chết theo**. Cách ly bảo mật thì có, cách ly hiệu năng thì không.

**Cách đóng.** `Max Pool Size` đặt tường minh theo hãng trong sổ đăng ký (một cột), tổng có chủ đích so với `max worker threads` của SQL Server; **hạn mức đồng thời theo hãng** ở gateway (bulkhead) để một hãng không chiếm hết capacity; `CommandTimeout` tường minh cho mọi truy vấn báo cáo; giám sát theo hãng (số kết nối, thời gian chờ pool) chứ không chỉ tổng.

## S-15 · HIGH · `.AcrossTenants(lyDo)` — "grep ra được" không phải là một cơ chế

**Điểm yếu.** AD-5 cho phép vượt phạm vi qua `.AcrossTenants(lyDo)`, kèm ba lời hứa: "có quyền riêng, có ghi vết, và grep ra được". Hai vấn đề: (1) **AD-15 không có bài test nào kiểm `.AcrossTenants`** — nó không nằm trong danh sách bảy mục; (2) "grep ra được" mô tả một thao tác thủ công của con người, không phải một cổng chặn. Đây là **API duy nhất trong hệ thống được thiết kế để đọc dữ liệu nhiều hãng**, nên nó xứng đáng là thứ được bảo vệ chặt nhất, không phải thứ có bình luận đẹp nhất.

Ai cầm "quyền riêng" đó? Một quản trị viên hãng có thể tự cấp cho mình không? Câu trả lời không có trong tài liệu, và đó chính là câu hỏi quyết định.

**Cách đóng.** (a) `.AcrossTenants` **không tồn tại trong bề mặt gọi được** của tầng Application các dịch vụ nghiệp vụ — đặt ở một assembly riêng, `InternalsVisibleTo` một danh sách trắng, và AD-15 đỏ build khi assembly khác tham chiếu; (b) quyền tương ứng **chỉ cấp được từ DB quản trị**, không từ màn quản trị của hãng — tức là một quản trị viên hãng **không thể** tự cấp; (c) dùng **login SQL riêng** (S-12), nên ngay cả code gọi đúng cũng bị SQL chặn nếu chưa cấp quyền; (d) **mọi lần gọi sinh một cảnh báo**, không chỉ một dòng log.

## S-16 · HIGH · 350 bảng chưa phân hạng: AD-15 nói đỏ build, phần "Deferred" nói cứ để đó — hai câu mâu thuẫn nhau

**Điểm yếu.** AD-15 liệt kê "có thực thể chưa phân loại hạng tenant" là điều kiện làm đỏ build. Bảng Deferred lại hoãn "phân loại hạng tenant cho 350 bảng chưa rõ" tới "trước mỗi lần thêm một vùng nghiệp vụ mới". Hai câu này **không thể cùng đúng**, và khi một mâu thuẫn như vậy gặp áp lực tiến độ, thứ bị bỏ luôn là bài test. Kết quả điển hình: danh sách ngoại lệ của AD-15 phình ra, và mặc định lặng lẽ đổi từ **fail-closed** sang **fail-open**.

Thêm nữa: hạng **"liên kết chéo"** được đặt tên trong AD-5 nhưng **không có luật nào đi kèm**. Đó lại chính là hạng duy nhất mà dòng của hãng A và hãng B nằm cạnh nhau — hạng nguy hiểm nhất, và là hạng không có quy tắc.

**Cách đóng.** Sửa cách diễn đạt để hai chỗ khớp nhau: *"Thực thể chưa phân hạng **tồn tại được** trong DB nhưng **không ánh xạ được** trong code — không có kiểu, không có repository, không truy vấn được. Phân hạng là điều kiện để chạm vào bảng, không phải điều kiện để build."* Và bổ sung luật cho hạng liên kết chéo: mọi truy vấn trên bảng liên kết chéo **bắt buộc** lọc theo tenant ở cả hai đầu quan hệ, phải khai báo tường minh, và phải có test rò rỉ riêng cho từng bảng.

---

# Phần D — Cache

## S-17 · CRITICAL · Ngoại lệ "cache dùng chung cho dữ liệu bất biến theo tenant" là khe rò rỉ **thiết kế sẵn** và AD-15 **không thể** kiểm nó

**Điểm yếu.** AD-6 xây một kiểu `CacheKey` không dựng được nếu thiếu tenant — rất tốt — rồi **tự khoét một cửa**: "Dữ liệu bất biến theo tenant (tỉnh thành, loại xe) mới được dùng key dùng chung, và phải khai báo tường minh". Ba vấn đề chồng lên nhau:

1. **Bài test AD-15 mù với cửa này.** AD-15 kiểm "`CacheKey` dựng thiếu tenant". Nhưng một key dùng chung được dựng **hợp lệ** qua chính API đó. Cơ chế ép duy nhất của AD-6 không nhìn thấy đúng cái nó cần canh.
2. **Ví dụ đã sai ngay trong câu.** "Loại xe" trong lược đồ này gần như chắc chắn **không** bất biến theo tenant — mỗi hãng có danh mục loại xe, hạng xe, mức giá riêng. Nếu chính tài liệu kiến trúc đưa một ví dụ sai, mọi người sẽ sao chép nó. Một thực thể bị gán nhầm hạng ở đây là một **kênh rò rỉ vĩnh viễn**, không phải một lần trượt.
3. **Ai nạp ô cache dùng chung?** Bộ khung không nói. Cách làm mặc định là "request nào tới trước thì nạp" — nghĩa là giá trị phục vụ cho **cả 18 hãng** được **lấy từ DB của một hãng**. Ngay cả với dữ liệu thật sự bất biến, đây là một đường **đầu độc cache**: bất cứ điều gì tác động được vào dữ liệu đó ở hãng A (một bản ghi bị sửa, một proc trả sai, một hàng thừa) sẽ hiện ra ở 17 hãng còn lại.

**Cách đóng.** Viết lại AD-6, phần ngoại lệ:
- Cache dùng chung **không bao giờ được nạp từ đường request của một tenant**. Nó nạp bởi một job nền, từ **một nguồn thẩm quyền duy nhất** — DB quản trị hoặc một schema tham chiếu riêng — và ở đường request nó **chỉ đọc**.
- Khoá dùng chung không dựng được từ API thông thường: nó dựng từ một **sổ đăng ký dữ liệu tham chiếu** khai báo tĩnh (`SharedReferenceData.Provinces`), và **danh sách đó là hữu hạn, nằm trong một file duy nhất**. AD-15 kiểm: mọi khoá dùng chung phải xuất phát từ một phần tử của sổ đó — **đây mới là bài test mà AD-15 đang thiếu**.
- Thêm một mục vào sổ đòi **chữ ký duyệt của người chịu trách nhiệm an ninh** — vì mỗi mục là một quyết định "dữ liệu này 18 hãng cùng thấy".
- Bỏ "loại xe" khỏi ví dụ. Dùng ví dụ thực sự bất biến: đơn vị hành chính, múi giờ, mã tiền tệ.

## S-18 · HIGH · Redis là kho gộp chung của cả 18 hãng và bộ khung không nói gì về bảo vệ nó

**Điểm yếu.** Sơ đồ topo có **một Redis** cho cả cache lẫn backplane SignalR, dùng chung cho 18 hãng. Bộ khung không nói: có bật TLS không, có AUTH/ACL không, ở mạng nào, ai đọc được. Tiền lệ trong ADR-004 còn thêm **Data Protection key ring trên Redis** — tức là Redis giữ luôn khoá bảo vệ cookie affinity và bất cứ payload nào được Data Protection bảo vệ.

Hệ quả: dù 18 database được tách rất kỷ luật, **Redis là nơi dữ liệu 18 hãng nằm cạnh nhau, không có ranh giới nào ngoài tiền tố chuỗi**. Một lệnh `SCAN`/`KEYS` từ bất cứ thứ gì chạm được Redis là đọc được cache của mọi hãng; một `FLUSHALL` là sự cố toàn hệ; và khoá Data Protection bị đọc là cookie giả được.

**Cách đóng.** Redis đòi TLS và **ACL user riêng cho từng dịch vụ**, mỗi user giới hạn theo mẫu khoá (`~{môi-trường}:*`) và giới hạn lệnh (cấm `KEYS`, `FLUSHALL`, `CONFIG`); đặt trong phân đoạn mạng riêng, không chạm được từ tầng web; **backplane và cache dùng instance/logical DB khác nhau** (khác đặc tính rủi ro, khác chính sách eviction); **Data Protection key ring tách khỏi Redis cache** hoặc ít nhất dùng ACL user khác; và nguyên tắc nội dung: **không cache PII thô** — cache khoá và dữ liệu đã chiếu, không cache số điện thoại khách và thông tin thanh toán.

## S-19 · HIGH · Không có luật `Cache-Control` cho phản hồi đã xác thực — tầng trung gian có thể tự tạo ra rò rỉ chéo

**Điểm yếu.** Chuỗi đường đi là Trình duyệt → HAProxy → YARP → BFF, cộng một CDN cho file tĩnh. Bộ khung không nói một câu nào về header cache của phản hồi API. Nếu bất kỳ tầng trung gian nào (HAProxy, YARP, một CDN đặt trước nhầm, một proxy doanh nghiệp phía khách, hoặc chính trình duyệt trên máy dùng chung) cache một phản hồi JSON đã xác thực **theo URL**, thì hai người dùng khác hãng gọi cùng một URL sẽ nhận cùng một phản hồi. Đây là kiểu rò rỉ không đi qua bất kỳ dòng code nào của đội phát triển, nên không bài test kiến trúc nào bắt được.

Thêm: AD-12 nói "một màn hình nên là một lời gọi BFF" — các lời gọi BFF sẽ là những URL ổn định, dễ đoán, giống hệt nhau giữa các tenant (vì tenant nằm trong token chứ không trong URL). Đó chính là điều kiện lý tưởng để một tầng cache trung gian nhầm lẫn.

**Cách đóng.** Vào mục Consistency Conventions: **mọi phản hồi API mang `Cache-Control: no-store, private` và `Vary: Authorization`**, đặt bởi middleware ở một chỗ duy nhất, không phải theo từng endpoint; gateway và HAProxy cấu hình tường minh **không cache** cho các route `/api/*`; CDN chỉ phục vụ file tĩnh tenant-agnostic và **không bao giờ đứng trước một route API**. Thêm một smoke test kiểm header trên mỗi dịch vụ.

## S-20 · MEDIUM · Tem `permissionVersion` trên khoá cache là phán đoán tại từng chỗ gọi, và khoá chưa có chiều người dùng

**Điểm yếu.** AD-6: "Mọi key mang tem `permissionVersion` **khi nội dung phụ thuộc quyền**". Mệnh đề "khi" biến nó thành một quyết định thủ công ở mỗi lần dùng, không kiểm được tự động, và sai theo chiều nguy hiểm (quên tem ⇒ phục vụ dữ liệu vượt quyền). Ngoài ra khuôn khoá `{hãng}:{companyId}:{vùng}:{khoá}` **không có chiều người dùng**: hai người dùng cùng công ty nhưng khác vai trò dùng chung ô cache trừ khi `permissionVersion` khác nhau — mà bộ khung không nói `permissionVersion` là theo người dùng hay theo công ty. Nếu theo công ty thì đây là leo thang quyền **trong nội bộ một hãng**.

**Cách đóng.** Định nghĩa dứt khoát `permissionVersion` **theo người dùng**; đảo mặc định: khoá **luôn** mang tem quyền, và muốn bỏ tem thì phải khai báo tường minh (`CacheScope.TenantWide` kèm lý do) — giống hệt cách AD-15 xử lý danh sách ngoại lệ. Bổ sung `userId` vào khuôn khoá cho mọi vùng chứa dữ liệu lọc theo quyền.

---

# Phần E — SignalR

## S-21 · HIGH · Tiền tố backplane theo hãng có thể **không thực hiện được** như đã viết

**Điểm yếu.** AD-7 (và ADR-002) yêu cầu "Backplane Redis có tiền tố `{môi-trường}:{hãng}`". Trong ASP.NET Core SignalR, tiền tố kênh Redis là **`RedisOptions.ChannelPrefix`, một giá trị cấu hình cho cả ứng dụng**, đặt lúc khởi động. Một tiến trình phục vụ 18 hãng không có cách tự nhiên nào để có 18 tiền tố — trừ khi dựng 18 kết nối backplane và 18 hub riêng, tức là đã trượt sang phương án B của ADR-002 mà không ai chọn nó.

Nghĩa là: **hoặc quy tắc này chưa được xác minh về mặt kỹ thuật, hoặc nó sẽ lặng lẽ không được thực hiện** khi lập trình viên gặp API thật. Một quy tắc bảo mật không thực hiện được còn nguy hiểm hơn không có quy tắc, vì nó tạo cảm giác đã có phòng thủ.

Cộng thêm câu hỏi ADR-002 đã nêu và bộ khung **đánh rơi**: web cũ vẫn chạy SignalR 2.2.0. **Hai stack realtime có dùng chung Redis không, và ai làm chủ không gian khoá?** Nếu dùng chung mà tiền tố va nhau thì đó là đường phát tin chéo giữa hai hệ.

**Cách đóng.** Xác minh bằng một spike kỹ thuật **trước khi chốt bộ khung**. Nếu tiền tố theo hãng không khả thi: giữ tiền tố theo **môi trường** (khả thi), và chuyển toàn bộ gánh nặng cách ly sang **tên group** — vốn đã mang `{hãng}:{companyId}:{chủ-đề}` và vốn đã là cơ chế thật. Đồng thời tách instance Redis backplane của WEB2 khỏi mọi thứ của web cũ — dứt khoát, không chia không gian khoá.

## S-22 · HIGH · Token đi qua query string lúc bắt tay WebSocket, và nó sẽ nằm trong log

**Điểm yếu.** Trình duyệt không đặt được header `Authorization` cho WebSocket, nên SignalR JS client gửi token qua **`?access_token=...`**. Hệ quả trong topo này: token của mọi phiên realtime sẽ xuất hiện trong **access log của HAProxy, log của IIS/ASP.NET Core Module, log request của YARP**, và trong mọi trace OpenTelemetry ghi URL đầy đủ (AD-13). Bộ khung không nhắc gì tới điều này, mà nó lại giao nhau với đúng phần AD-13 đang lưu 100% lệnh ghi và lấy mẫu phần còn lại.

Đây không phải rò chéo tenant trực tiếp, nhưng nó biến **quyền đọc log vận hành thành quyền mạo danh người dùng của bất kỳ hãng nào** — và log là thứ nhiều người được đọc hơn database.

**Cách đóng.** (a) Đổi sang **vé kết nối dùng một lần**: FE gọi một endpoint đã xác thực để lấy một ticket TTL ~30 giây, chỉ dùng được cho `/signalr`, và chính ticket đó đi vào query string; (b) **che tại nguồn**: HAProxy, IIS và YARP đều cấu hình xoá/thay tham số `access_token` trước khi ghi log, và OpenTelemetry cấu hình không ghi query string cho route `/signalr/*`; (c) đưa việc này vào **AD-22** (che PII và bí mật trong log) thay vì để nó là một mẹo cấu hình không ai nhớ.

## S-23 · HIGH · Kết nối realtime sống lâu hơn quyền — thu hồi không chạm tới nó

**Điểm yếu.** AD-7 phân giải tenant lại mỗi lần gọi hub method **từ `Context.Items`** — tức là từ giá trị đã ghi **lúc bắt tay**. Đúng về tenant, nhưng nó **không** xử lý chuyện quyền thay đổi sau đó. Một kết nối WebSocket sống hàng giờ (HAProxy đang được yêu cầu tăng `timeout tunnel` cho đúng mục đích đó). Trong khoảng thời gian ấy: token hết hạn, người dùng bị khoá, quyền bị rút, `permissionVersion` tăng — **và luồng dữ liệu bản đồ xe vẫn chảy về trình duyệt đó**. Ngân sách "độ trễ thu hồi" (S-08) bị vỡ ở đúng kênh dữ liệu dày nhất.

Biến thể nguy hiểm hơn: **đổi công ty**. AD-7 nói "Đổi công ty ⇒ rời group cũ, vào group mới". Nếu việc rời group do **client** khởi xướng thì một client không rời group cũ sẽ **nhận đồng thời dữ liệu của cả hai công ty** — và đó chính là hình dạng lỗi `registerCompanyIds` mà ADR-002 đã loại bỏ, quay lại từ cửa sau.

**Cách đóng.** (a) Bật `CloseOnAuthenticationExpiration` để kết nối bị đóng khi token hết hạn, kèm access token TTL ngắn (S-08) — đó là cơ chế làm cho ngân sách thu hồi đúng cả với SignalR; (b) **thành viên group chỉ do server quyết định** trong `OnConnectedAsync` từ token; client **được thu hẹp, không bao giờ được mở rộng** — viết câu này vào AD-7, vì ADR-002 có mà bộ khung đã lược mất; (c) đổi công ty ⇒ **server chủ động ngắt kết nối cũ**, client kết nối lại bằng token mới; đừng phụ thuộc vào client gọi `LeaveGroup`; (d) khi `permissionVersion` tăng, đẩy một tín hiệu ngắt tới mọi kết nối của người dùng đó.

## S-24 · MEDIUM · `{chủ-đề}` trong tên group và cookie affinity

**Điểm yếu.** Hai chi tiết nhỏ, cùng lớp "chuỗi ghép":
- Tên group `{hãng}:{companyId}:{chủ-đề}` sinh bởi một hàm duy nhất — tốt — nhưng nếu `{chủ-đề}` đến từ đầu vào client thì một chủ đề chứa `:` có thể tạo ra tên group thuộc không gian tenant khác (cùng lớp với S-05).
- `SessionAffinity: Cookie / .Yarp.Affinity` (ADR-004) chọn instance phục vụ. Cookie này phải được ký/mã hoá và không được dùng lại key ring chung với thứ khác (nối với S-18).

**Cách đóng.** `{chủ-đề}` là một **enum đóng**, không phải chuỗi; hàm sinh tên group nhận `TenantCode` + `int companyId` + `Topic` (đều là kiểu, không phải `string`) — khi đó việc chèn dấu phân cách không biểu đạt được. Cookie affinity dùng Data Protection với purpose riêng, `Secure`, `HttpOnly`, `SameSite=Lax` trở lên.

---

# Phần F — Ghi vết (AD-11)

## S-25 · CRITICAL · Ghi vết lấy tenant từ **cùng cái nguồn có thể đang sai** — nên nó không phát hiện được đúng lớp lỗi mà nó tồn tại để bù đắp

**Điểm yếu.** AD-11 đặt ghi vết làm khoản bù cho last-write-wins, và AD-13 ghi "cả tenant thực thi lẫn tenant hiệu lực". Nhưng **cả hai giá trị đều đến từ `TenantContext`**. Nếu context sai (S-11, S-13) thì lệnh ghi **và** dòng ghi vết của nó **cùng sai theo một hướng**. Hồ sơ kiểm toán sẽ nói "hãng B ghi vào hãng B", trong khi dữ liệu thật của hãng A vừa bị đẩy sang DB hãng B. **Không có truy vấn nào trên tập ghi vết đó phát hiện ra sự việc.**

Đây là điểm yếu nghiêm trọng nhất của toàn bộ chiến lược phát-hiện-sau: cơ chế phát hiện chia chung một điểm hỏng với cơ chế phòng ngừa.

**Cách đóng.** Thêm **AD-20**: mỗi dòng ghi vết mang **hai nhóm trường độc lập nguồn**:
- Từ ngữ cảnh ứng dụng: `tenant`, `companyId`, `userId`, `corrId`, `jti`.
- **Từ chính kết nối, đo tại thời điểm ghi**: tên server SQL, `DB_NAME()`, `ORIGINAL_LOGIN()`, `SUSER_SNAME()`, `APP_NAME()`.

Rồi một job đối chiếu định kỳ (và một cảnh báo thời gian thực) chạy đúng một bất biến: **tenant trong ngữ cảnh phải ánh xạ tới đúng DB vật lý đã ghi**. Lệch ⇒ cảnh báo mức sự cố. Đây là bài kiểm tra duy nhất trong cả thiết kế thực sự phát hiện được một lần ghi lệch tenant, và nó rẻ.

## S-26 · CRITICAL · Ghi vết chỉ phủ **lệnh ghi** — nhưng hậu quả tệ nhất đã nêu là **rò dữ liệu**, tức là một lệnh **đọc**

**Điểm yếu.** AD-11: "**mọi lệnh ghi** bắt buộc ghi vết". Bối cảnh lại nói rõ "một vụ rò dữ liệu tenant là hậu quả tệ nhất có thể". Rò dữ liệu là **đọc**. Theo thiết kế hiện tại, nếu hãng B đọc được dữ liệu hãng A — qua cache dùng chung (S-17), qua `.AcrossTenants` (S-15), qua một phản hồi bị cache (S-19), hay qua context sai (S-13) — thì **không có một dòng nào được ghi ở bất kỳ đâu**. Sự việc vô hình cả lúc xảy ra lẫn sau đó. Trong một cuộc điều tra sau sự cố, câu hỏi đầu tiên của khách hàng là "dữ liệu của chúng tôi có bị ai xem không", và hệ thống này sẽ không trả lời được.

**Cách đóng.** Trong AD-20, mở rộng phạm vi ghi vết:
- **100% các lần đọc vượt phạm vi** (`.AcrossTenants`) — đầy đủ câu truy vấn, số dòng trả về, người gọi, lý do.
- **Đọc các bảng chứa dữ liệu cá nhân** (khách, tài xế, thanh toán) ghi ở mức truy vấn: ai, khi nào, bao nhiêu dòng, tenant nào. Không cần nội dung dòng — số lượng và phạm vi là đủ để phát hiện bất thường.
- **Kết xuất số lượng lớn** (export Excel, báo cáo trả > N dòng) là một sự kiện riêng có ngưỡng cảnh báo — đây là hình dạng của một vụ mang dữ liệu ra ngoài.
- Bổ sung **cảnh báo dựa trên khối lượng**: một tài khoản đọc nhiều dòng bất thường so với chính nó, hoặc so với các tài khoản cùng vai trò.

## S-27 · HIGH · Ghi vết nằm ở đâu, ai sửa được, giữ bao lâu — chưa có câu trả lời

**Điểm yếu.** AD-11 không nói kho ghi vết nằm ở đâu. Hai khả năng, cả hai đều có vấn đề chưa được xử lý:
- **Trong DB của hãng**: thì một lệnh ghi đi lạc DB cũng ghi vết vào DB lạc (nối tiếp S-25); **và** login ứng dụng có quyền ghi bảng đó cũng có quyền `UPDATE`/`DELETE` nó, nên chứng cứ sửa được bởi chính thứ bị điều tra; **và** web cũ chạy trên cùng DB có thể chạm vào nó.
- **Trong DB quản trị**: tốt hơn về tính toàn vẹn, nhưng nó lại thành nút thắt ghi và là một đường phụ thuộc mới cho mọi lệnh ghi (nối với rủi ro điểm chết đơn lẻ đã ghi nhận ở AD-14).

Cũng vắng: thời gian lưu, tính bất biến, ai được đọc (bản thân kho ghi vết chứa dữ liệu của cả 18 hãng — xem S-31), và **có ai truy vấn nó không**. Một hồ sơ kiểm toán không có truy vấn phát hiện, không có cảnh báo, không có chủ sở hữu thì không phải một biện pháp kiểm soát; nó là một cái kho.

**Cách đóng.** Trong AD-20: kho ghi vết **chỉ-ghi-thêm**, tách khỏi 18 DB khách, ghi bằng **một login SQL chỉ có quyền `INSERT`** (không `UPDATE`, không `DELETE`); chống sửa bằng SQL Server Ledger / temporal table / chuỗi băm; ghi bất đồng bộ qua hàng đợi bền để không thành nút thắt, nhưng **mất kết nối kho ghi vết ⇒ từ chối lệnh ghi** (fail closed) với các thao tác nhạy cảm; thời gian lưu tối thiểu **12 tháng**, khai báo tường minh; kèm **danh sách truy vấn phát hiện cụ thể** (ít nhất: bất biến S-25, mọi lần `.AcrossTenants`, đăng nhập thất bại chéo mã hãng, kết xuất lớn), mỗi truy vấn có ngưỡng, có cảnh báo, có người trực.

## S-28 · HIGH · Không phân biệt được **ai** đã ghi khi hai ứng dụng cùng ghi một DB

**Điểm yếu.** Rủi ro đã chấp nhận số 1 là "hai ứng dụng cùng ghi ⇒ mất dữ liệu âm thầm", bù bằng ghi vết. Nhưng **web cũ không ghi vết theo khuôn của WEB2**. Nên khi một bản ghi bị mất, hồ sơ chỉ có một nửa câu chuyện: WEB2 ghi lúc nào, còn bên kia thì không biết gì. Khoản bù đắp cho rủi ro chấp nhận số 1 **không thực sự bù được** — nó chỉ chứng minh WEB2 đã làm gì, không dựng lại được trình tự.

**Cách đóng.** Ở mức rẻ nhất và hiệu quả ngay: **mỗi ứng dụng một login SQL và một `Application Name` riêng** trong chuỗi kết nối (và web cũ cũng đổi — đây là một thay đổi config, không phải code). Khi đó mọi công cụ phía SQL (`sys.dm_exec_sessions`, Extended Events, CDC, trigger kiểm toán) đều **quy trách nhiệm được**. Với những bảng tranh chấp cao (AD-10/ADR-004 đã nêu luật "một bảng, một người ghi"), thêm cột `ModifiedBy`/`ModifiedApp`/`RowVersion` — đều là cột `NULL`-able mới, hợp lệ theo AD-10.

---

# Phần G — Bí mật và sổ đăng ký hãng

## S-29 · CRITICAL · Sổ đăng ký là tài sản giá trị nhất của hệ thống và đang được mô tả bằng một dòng

**Điểm yếu.** AD-14: "Connection string **mã hoá trong cột**, khoá nằm ngoài mã nguồn." Đó là toàn bộ những gì bộ khung nói về thứ mà, nếu lộ, cho phép đọc **dữ liệu của cả 18 hãng cùng lúc**. ADR-004 còn nghiêm túc hơn (nêu Key Vault / Vault / DPAPI) nhưng bộ khung đã rút gọn đi.

Các câu hỏi chưa có chỗ nào trả lời: khoá nằm ở đâu và ai đọc được; có xoay khoá không và bằng quy trình nào; mật khẩu SQL có xoay không; ai được `SELECT` bảng đó; việc đọc sổ đăng ký có được ghi vết không; ai được thêm một dòng (thêm một dòng = tạo một tenant = một đường leo thang rất mạnh nếu kiểm soát lỏng).

**Điểm đáng lo nhất, và nó đến từ chính AD-14:** AD-14 **bắt buộc cache cục bộ trong tiến trình** để chịu được lúc sổ đăng ký chết. Nghĩa là **mỗi tiến trình ứng dụng giữ 18 connection string đã giải mã trong RAM suốt vòng đời**. Từ đó: một crash dump ghi ra ổ đĩa, một endpoint chẩn đoán trả về cấu hình, một lỗi lộ bộ nhớ, hay một người vận hành lấy dump để gỡ lỗi — **đều là lộ toàn bộ 18 hãng**. Tính sẵn sàng vừa được mua bằng bán kính ảnh hưởng.

**Cách đóng.** Mở rộng AD-14 thành một AD bảo mật thật sự:
- **Ưu tiên tuyệt đối: bỏ mật khẩu.** Dùng Windows Authentication / Managed Identity cho từng DB hãng. Không có mật khẩu thì không có gì để lộ, và S-29 phần lớn biến mất. Đây là lựa chọn kiến trúc tốt nhất trong cả bản rà soát này về tỉ lệ lợi ích/chi phí.
- Nếu buộc phải có mật khẩu: **mã hoá kiểu phong bì** (khoá dữ liệu mã hoá bởi khoá gốc trong KMS/DPAPI gắn máy); **cache dạng đã mã hoá**, chỉ giải mã ngay trước khi mở kết nối và không giữ tham chiếu; cấm ghi ra log ở mọi mức; **tắt crash dump ghi đĩa** hoặc giới hạn quyền đọc dump; **không có endpoint chẩn đoán/cấu hình** nào trên môi trường thật.
- **Least privilege thật** (nối S-12): mỗi chuỗi kết nối chỉ mở được đúng một DB với quyền tối thiểu — một chuỗi lộ là một hãng, không phải 18.
- DB quản trị nằm **phân đoạn mạng riêng**, chỉ Auth API và connection factory chạm tới; **mọi lần đọc sổ đăng ký được ghi vết**; thêm/sửa một dòng đòi quy trình bốn mắt.
- **Quy trình xoay mật khẩu** viết sẵn và diễn tập: cache cục bộ phải bị vô hiệu trên mọi instance khi một dòng đổi, nếu không việc xoay khoá sẽ làm sập đăng nhập của một hãng.
- **Vô hiệu hoá một hãng phải là một công tắc thật**: đặt cờ tắt trên một dòng sổ đăng ký phải **cắt cả các phiên đang chạy**, không chỉ chặn đăng nhập mới. Đây cũng là công cụ ứng cứu sự cố duy nhất có sẵn ở tầm hệ thống.

---

# Phần H — "Cấm" mà không có cơ chế ép

## S-30 · HIGH · AD-15 là chốt chặn duy nhất, và nó phủ **2 trên 9** điều cấm của bộ khung

Đây là điểm yếu mang tính hệ thống chứ không phải một lỗi cụ thể: AD-15 tồn tại để "mọi AD ở trên không thoái hoá thành lời khuyên", nhưng danh sách bảy bài test của nó chỉ chạm tới một phần nhỏ các điều cấm đã viết.

| Điều cấm | Ở đâu | AD-15 có kiểm? | Kiểm được bằng gì |
|---|---|---|---|
| `new SqlConnection` ngoài factory | AD-2 | ✅ | Đã có — **mở rộng thêm**: `SqlConnectionStringBuilder`, `DbProviderFactory`, đọc connection string từ `IConfiguration` |
| Nhận `tenantId`/`companyId` từ request | **AD-4** | ❌ | Quét tên tham số + thuộc tính DTO; model binder chặn toàn cục (S-06) |
| Endpoint liệt kê hãng khi chưa xác thực | AD-3 | ❌ | Duyệt bảng route: mọi route ẩn danh phải nằm trong danh sách trắng (S-02) |
| Cache key dùng chung sai chỗ | AD-6 | ⚠️ một phần | Khoá dùng chung phải xuất phát từ sổ `SharedReferenceData` (S-17) |
| `Clients.All` / `Others` / `AllExcept` | AD-7 | ✅ | Đã có — **mở rộng**: `IHubContext` chỉ được tham chiếu trong assembly bọc; tên group chỉ sinh từ một hàm |
| Trả HTTP 200 cho lỗi | AD-8 | ❌ | Test hợp đồng trên middleware + smoke test mỗi dịch vụ |
| Dynamic SQL nối chuỗi | AD-9 | ❌ | Analyzer cấm nội suy chuỗi vào `CommandDefinition`/`Execute`; chỉ tham số Dapper |
| Đổi tên/xoá cột, đổi kiểu | AD-10 | ❌ | Lint thư mục migration của grate: chặn `DROP COLUMN`, `ALTER COLUMN`, `sp_rename` |
| BFF chứa nghiệp vụ / chạm DB | AD-12 | ❌ | Đồ thị tham chiếu: `Staxi.Bff.Web` không được tham chiếu `*.Infrastructure` hay `Microsoft.Data.SqlClient` |
| Log dữ liệu nhạy cảm | Conventions | ❌ | Xem S-31 |
| `.AcrossTenants` ngoài danh sách trắng | AD-5 | ❌ | Giới hạn assembly + `InternalsVisibleTo` (S-15) |
| Test rò rỉ hai tenant | ADR-002 yêu cầu | ❌ **đã rơi mất** | Xem S-34 |

**Cách đóng.** Viết lại AD-15 thành **bảng ánh xạ một-một: mỗi điều "cấm" trong bộ khung ⇄ đúng một cơ chế ép có tên**. Và thêm một quy tắc về chính bộ khung: *"Không được thêm một điều cấm mới mà không nêu cơ chế ép ngay trong cùng một AD."* Nếu không ép được thì viết nó ở mục Rủi ro đã chấp nhận, đừng viết ở mục Invariants — như thế mọi người biết đâu là tường, đâu là biển báo.

---

# Phần I — Những thứ vắng mặt hoàn toàn

## S-31 · HIGH · Kho quan trắc là **nơi duy nhất trong hệ thống dữ liệu 18 hãng nằm chung** — và nó chưa được phân loại như vậy

**Điểm yếu.** Cả kiến trúc dựng trên tiền đề "18 database tách riêng". Nhưng AD-13 gom **trace, metrics và log của cả 18 hãng vào một hệ quan trắc duy nhất**, lưu **100% lệnh ghi**, kèm `corrId`, tenant, user, bảng, khoá bản ghi (AD-11). Cộng với S-22 (token trong URL) và bất kỳ payload nào lọt vào trace, kho log trở thành **bản sao thứ 19 của dữ liệu, không tách tenant**, ở một nơi thường có kiểm soát truy cập lỏng hơn database rất nhiều — vì "chỉ là log".

Convention hiện tại chỉ có một dòng: "cấm log dữ liệu nhạy cảm (PII, số thẻ); chính sách che viết trước khi bật trace". Đó là một lời hứa không có chủ, không có hạn, và không có cơ chế — nằm đúng vào nhóm S-30.

**Cách đóng.** Thêm **AD-22 — kho quan trắc mang mức phân loại cao nhất của hệ**:
- Danh sách trường **cho phép ghi (allow-list)**, không phải danh sách cấm: log ghi **khoá và định danh**, không ghi nội dung dòng dữ liệu. Payload request/response mặc định **không ghi**.
- Bộ lọc che chạy ở **tầng sink của Serilog/OTel**, một chỗ duy nhất, có test đơn vị với các mẫu thật (số điện thoại VN, biển số, CMND/CCCD, số thẻ, `access_token`, `Authorization`, connection string).
- **Mọi bản ghi log mang nhãn tenant**, và kiểm soát truy cập kho log theo vai trò; ai đọc được log 18 hãng phải là một danh sách ngắn, có tên, và **việc đọc log cũng được ghi vết**.
- Thời gian lưu tường minh, và **log phải xoá được theo chủ thể dữ liệu** (nối S-33) — điều này chỉ khả thi nếu không ghi payload thô, nên nó củng cố gạch đầu dòng đầu.
- Chính sách che **hoàn thành trước khi bật trace trên môi trường có dữ liệu thật**, có người chịu trách nhiệm và một ngày cụ thể.

## S-32 · HIGH · Biên trình duyệt chưa được định nghĩa: token lưu ở đâu, CSRF, CORS, CSP

**Điểm yếu.** Bộ khung nói "JWT stateless, không dùng server session" và dừng ở đó. Với một SPA, các câu hỏi quyết định nằm ở phía trình duyệt và **không có câu nào trả lời**:

- **Token lưu ở đâu?** `localStorage` thì một lỗi XSS bất kỳ trên domain đó lấy được token của người dùng đang đăng nhập; và vì **cả 18 hãng dùng chung một domain**, một XSS lưu trữ (stored XSS) nằm trong dữ liệu dùng chung sẽ chạy trên phiên của người dùng **mọi hãng**. Đây là đường rò chéo tenant **không đi qua database** và bộ khung không có lớp phòng thủ nào cho nó.
- **CSRF.** Nếu mọi thứ là `Authorization: Bearer` thì CSRF gần như không áp dụng — nhưng ADR-004 đã đưa vào một **cookie** (`.Yarp.Affinity`), và SignalR/refresh token nhiều khả năng cũng cần cookie. Ngay khi có một route xác thực bằng cookie, CSRF sống lại, và không có câu nào nói về nó.
- **CORS.** Một domain mới + CDN + gateway: danh sách origin phải khai báo đóng. `AllowAnyOrigin` kèm credentials, hay phản chiếu origin, là một lỗi cấu hình một dòng.
- **CSP.** Không nhắc tới. Mà ADR-004 lại chốt "thương hiệu/màu/logo từng hãng là **dữ liệu từ API config**" — tức là **giá trị do dữ liệu tenant điều khiển được nhúng vào DOM/CSS**. Một URL logo hay một chuỗi màu không được kiểm sẽ là XSS lưu trữ, và nạn nhân là mọi người dùng mở màn hình đó.
- **Chuỗi cung ứng CDN.** FE là file tĩnh trên CDN dùng chung với hệ cũ. Ai đẩy được file lên CDN thì **chạy được mã trên console quản trị của cả 18 hãng**.

**Cách đóng.** Thêm **AD-21 — biên trình duyệt**: access token **giữ trong bộ nhớ JS, không `localStorage`**; refresh token trong cookie `HttpOnly; Secure; SameSite=Strict`, phạm vi path đúng `/auth/refresh`; mọi route xác thực bằng cookie kiểm `Origin`/`Sec-Fetch-Site` ở gateway; **CORS danh sách trắng đóng**, cấm phản chiếu origin; **CSP không `unsafe-inline`, không `unsafe-eval`**, `frame-ancestors 'none'`, cộng HSTS, `X-Content-Type-Options`, `Referrer-Policy: no-referrer`; **giá trị thương hiệu của tenant phải qua allow-list kiểu dữ liệu** (màu khớp regex hex, logo là một `assetId` do hệ thống cấp chứ không phải URL tự do); **SRI cho mọi asset từ CDN** và quyền đẩy CDN giới hạn vào pipeline Jenkins, không phải tài khoản cá nhân.

## S-33 · HIGH · Sao lưu, khôi phục và luồng dữ liệu giữa môi trường — không có một chữ nào

**Điểm yếu.** 18 database nghĩa là 18 dòng sao lưu, và bộ khung liệt kê 4 môi trường (dev · devtest · staging · production) mà **không có luật nào về việc dữ liệu được phép chảy giữa chúng**. Các lớp yếu điểm thực tế:

- **Bản sao lưu là bản sao không kiểm soát của dữ liệu tenant.** Nếu 18 bản backup nằm chung một thư mục/ share với một bộ quyền, thì cách ly ở tầng SQL bị vô hiệu ở tầng file. Backup có mã hoá không? Khoá ở đâu (nếu cùng chỗ với backup thì không tính là mã hoá)?
- **Khôi phục nhầm chỗ.** Khôi phục DB hãng A đè lên tên DB hãng B là một thao tác gõ nhầm — và nếu sổ đăng ký vẫn trỏ theo tên DB thì **hệ thống sẽ phục vụ dữ liệu hãng A dưới danh nghĩa hãng B, không báo lỗi gì**. Cần một bất biến kiểm được (nối S-25: `DB_NAME()` phải khớp tenant).
- **Sao chép production xuống dev/staging để gỡ lỗi** — cách làm phổ biến nhất và là con đường rò dữ liệu khách hàng thường gặp nhất trong thực tế. Bốn môi trường mà không có luật là mặc định cho phép.
- **DB quản trị được sao lưu cùng bộ**, và bản sao lưu đó chứa 18 chuỗi kết nối đã mã hoá (S-29). Nếu khoá cũng nằm trong cùng máy/backup thì bán kính là toàn hệ.

**Cách đóng.** Thêm **AD-23**: sao lưu **mã hoá, khoá trong KMS tách khỏi nơi lưu backup**; **mỗi hãng một đường lưu và một bộ quyền riêng**; quy trình khôi phục bắt buộc kiểm chéo `DB_NAME()` ↔ dòng sổ đăng ký trước khi mở lưu lượng; **cấm tuyệt đối dữ liệu production chảy xuống dev/devtest** — dữ liệu phi-production phải được che/sinh giả, và staging nếu dùng dữ liệu thật thì phải chịu **đúng bộ kiểm soát của production**; diễn tập khôi phục có lịch, và bài diễn tập bao gồm kiểm tra cách ly tenant sau khôi phục, không chỉ "DB lên được".

## S-34 · HIGH · Không có kỳ vọng kiểm chứng an ninh — và **bài test rò rỉ hai tenant của ADR-002 đã rơi mất khỏi bộ khung**

**Điểm yếu.** ADR-002 yêu cầu rất rõ: *"Bài test rò rỉ hai-tenant nên là **một trong những test đầu tiên của dự án**, không phải test cuối"* — hai tenant giả, phát tin ở A, khẳng định B không nhận được gì. **Bộ khung không nhắc tới nó ở bất kỳ đâu**, kể cả trong danh sách AD-15. Yêu cầu kiểm chứng cụ thể và có giá trị nhất trong toàn bộ tập ADR đã biến mất trong quá trình chắt lọc.

Ngoài ra không có: SAST, quét phụ thuộc, quét bí mật trong git (repo này **đã từng** có file config bị commit — tiền lệ có thật), DAST, và không có kỳ vọng kiểm thử xâm nhập trước khi hãng đầu tiên lên production.

**Cách đóng.** Thêm **AD-24 — kiểm chứng an ninh**:
- **Bộ test rò rỉ hai tenant là hạ tầng test nền tảng**, viết trước tính năng đầu tiên, và phủ **cả bốn khe**: HTTP API, SignalR, cache, và kết nối DB. Khuôn mỗi bài: dựng ngữ cảnh tenant A, thực hiện thao tác, **khẳng định tenant B không quan sát được gì** — kể cả qua thời gian phản hồi và qua sự tồn tại của khoá cache.
- CI: quét bí mật (chặn commit), quét phụ thuộc (fail theo ngưỡng CVE), phân tích tĩnh, DAST trên môi trường staging.
- **Kiểm thử xâm nhập bên ngoài trước khi hãng đầu tiên lên production**, phạm vi nêu đích danh cách ly tenant; lặp lại trước hãng thứ ba (thời điểm bảng Deferred đã tự nhận bán kính ảnh hưởng thành đáng kể).
- Một bài diễn tập ứng cứu sự cố có kịch bản "nghi ngờ rò dữ liệu chéo hãng", chạy thử ít nhất một lần — nó sẽ phơi ra ngay việc S-26 làm ta không trả lời được câu hỏi của khách hàng.

## S-35 · HIGH · Lưu trữ dữ liệu, quyền của chủ thể dữ liệu và nghĩa vụ thông báo vi phạm — vắng mặt

**Điểm yếu.** Nền tảng này giữ dữ liệu cá nhân của hành khách và tài xế cho 18 công ty. Theo **Nghị định 13/2023/NĐ-CP** về bảo vệ dữ liệu cá nhân, mỗi hãng taxi là một **bên kiểm soát dữ liệu riêng**, còn đơn vị vận hành nền tảng ở vị trí bên xử lý — kéo theo nghĩa vụ hợp đồng về cách ly, thời hạn lưu trữ, quyền xoá của chủ thể, và **thông báo vi phạm trong 72 giờ**. Bộ khung không có một dòng nào về thời hạn lưu, về xoá theo yêu cầu, hay về đường thông báo sự cố **theo từng hãng**.

Điều này có hệ quả kiến trúc thật, không chỉ giấy tờ: xoá một chủ thể dữ liệu phải thực hiện được **trên DB hãng + cache Redis + kho log + kho ghi vết + bản sao lưu**. Nếu log giữ payload thô (S-31) thì nghĩa vụ đó không thực hiện nổi.

**Cách đóng.** Một mục "Nghĩa vụ dữ liệu" trong bộ khung: thời hạn lưu tường minh theo loại dữ liệu (nghiệp vụ / log / ghi vết / sao lưu); một đường xoá theo chủ thể chạy được trên đủ năm kho; **đường thông báo sự cố theo từng hãng** (ai gọi cho ai, trong bao lâu) — vì một sự cố chạm nhiều hãng cần 18 cuộc gọi riêng chứ không phải một thông báo chung; và ghi rõ trong hợp đồng với từng hãng rằng nền tảng dùng chung hạ tầng nhưng tách database.

## S-36 · HIGH · Lỗ hổng `ChangeCompany` của web cũ đang được xếp là "không phải việc của WEB2" — nhưng nó **giới hạn trần an toàn của WEB2**

**Điểm yếu.** Dòng cuối bảng rủi ro ghi `HomeController.ChangeCompany` không kiểm quyền, kết luận "Không phải việc của WEB2", kèm khuyến nghị vá sớm. Về mặt phân chia trách nhiệm thì hợp lý; về mặt bảo mật thì không, vì **web cũ chạy trên chính những database mà WEB2 bảo vệ**. Chừng nào web cũ còn chạy: một lỗ SQLi, một XSS, hay chính lỗ `ChangeCompany` ở web cũ **là một vụ rò dữ liệu của WEB2** theo đúng nghĩa với khách hàng — họ không phân biệt hai trang web. Mọi công sức của AD-2/4/5/6/7 chỉ nâng được sàn, không nâng được trần.

**Cách đóng.** Chuyển dòng này từ "không phải việc của WEB2" sang **một rủi ro kế thừa có chủ sở hữu và có hạn**: (a) xác minh trên hệ chạy thật **trong tuần này** — đây là lỗ hổng leo thang quyền có thể đang mở trên production, độc lập hoàn toàn với WEB2; (b) nếu đúng thì vá ở web cũ ngay, kèm khôi phục bộ lọc `.Where(p => p.FK_CompanyID == companyid)` đã biến mất trong `GetRoleLastest`; (c) ghi vào bộ khung rằng **mức cách ly thực tế của một hãng bằng mức thấp hơn giữa WEB2 và web cũ**, cho tới khi hãng đó rời hẳn web cũ — đó là một lập luận mạnh cho việc rút ngắn giai đoạn chạy song song.

## S-37 · LOW · `corrId` hiển thị cho người dùng

**Điểm yếu.** AD-8 cho hiện `corrId` trên màn lỗi để người dùng đọc cho CSKH. Nếu `corrId` sinh tuần tự hay chứa thông tin về tenant/instance thì nó rò rỉ khối lượng và cấu trúc hệ thống ở mức thấp.

**Cách đóng.** `corrId` là giá trị ngẫu nhiên (ULID/GUID v7), không mã hoá thông tin nào, không tương quan được giữa các tenant. Một dòng trong Consistency Conventions là đủ.

---

# Tổng hợp theo mức

| Mức | Mã | Tóm tắt |
|---|---|---|
| CRITICAL | S-06 | Luật "cấm nhận `companyId` từ request" không có trong AD-15 |
| CRITICAL | S-07 | Khoá ký token: thuật toán/khoá/xoay/tách môi trường đều chưa định nghĩa; bán kính là 18 hãng |
| CRITICAL | S-11 | Connection factory có thể nhận tenant từ chỗ gọi — test AD-15 không bắt được |
| CRITICAL | S-12 | Một login SQL dùng chung 18 DB: mọi lỗi code thành rò dữ liệu thay vì lỗi quyền |
| CRITICAL | S-13 | Ngữ cảnh tenant rò qua singleton-giữ-scoped và công việc chạy nền |
| CRITICAL | S-17 | Ngoại lệ cache dùng chung của AD-6: nạp từ một tenant, phục vụ cả 18, AD-15 mù |
| CRITICAL | S-25 | Ghi vết lấy tenant từ cùng nguồn có thể đang sai ⇒ không phát hiện được ghi lệch tenant |
| CRITICAL | S-26 | Ghi vết chỉ phủ lệnh ghi; một vụ **rò** (đọc) không để lại dấu vết nào |
| CRITICAL | S-29 | Sổ đăng ký: 18 chuỗi kết nối giải mã nằm trong RAM mọi tiến trình, không có quản lý khoá |
| HIGH | S-01 · S-02 · S-03 · S-04 | Liệt kê tenant qua thời gian và qua endpoint thương hiệu; rate limit không chặn stuffing chéo hãng; mật khẩu kế thừa chưa khảo sát |
| HIGH | S-08 · S-09 | Token cũ sống sau khi đổi công ty; độ trễ thu hồi chưa có ngân sách; token tạm chưa có phạm vi |
| HIGH | S-15 · S-16 | `.AcrossTenants` không có cơ chế ép; 350 bảng chưa phân hạng mâu thuẫn với AD-15 |
| HIGH | S-18 · S-19 | Redis gộp 18 hãng không có ACL; thiếu luật `no-store` cho phản hồi đã xác thực |
| HIGH | S-21 · S-22 · S-23 | Tiền tố backplane theo hãng có thể bất khả thi; token trong URL WebSocket vào log; kết nối sống lâu hơn quyền |
| HIGH | S-27 · S-28 | Kho ghi vết sửa được/không có truy vấn phát hiện; không quy trách nhiệm được giữa hai ứng dụng |
| HIGH | S-30 | AD-15 phủ 2/9 điều cấm |
| HIGH | S-31 · S-32 · S-33 · S-34 · S-35 · S-36 | Kho quan trắc gộp 18 hãng; biên trình duyệt (token/CSRF/CORS/CSP) trống; sao lưu & luồng dữ liệu môi trường trống; test rò rỉ hai tenant đã rơi mất; lưu trữ/PDPD trống; lỗ hổng web cũ giới hạn trần an toàn |
| MEDIUM | S-05 · S-10 · S-14 · S-20 · S-24 | Chuẩn hoá `tenantCode`; ràng buộc token; pool và noisy neighbour; tem quyền trên khoá cache; enum chủ đề & cookie affinity |
| LOW | S-37 | `corrId` phải ngẫu nhiên, không mang thông tin |

---

# Các AD đề nghị bổ sung

| AD mới | Nội dung | Đóng các mục |
|---|---|---|
| **AD-16** | `TenantCode` là value object: bộ ký tự đóng, một hàm chuẩn hoá duy nhất, không dựng từ `string` thô | S-05, S-24 |
| **AD-17** | `TenantContext` bất biến đặt một lần; factory không nhận tham số tenant; **một login SQL riêng cho mỗi hãng**; không context ⇒ ném lỗi; cấm `USE`/`ChangeDatabase`/tên ba phần | S-11, S-12, S-13 |
| **AD-18** | Vòng đời token: ký bất đối xứng + JWKS, `alg` đóng, khoá tách theo môi trường, TTL ≤ 10 phút, refresh quay vòng, `jti`, **ngân sách độ trễ thu hồi 60 giây**, token tạm có `aud` riêng | S-07, S-08, S-09, S-10 |
| **AD-19** | Chống liệt kê hãng: công-sức-hằng-số + sàn thời gian + phản hồi thất bại đồng nhất; rate limit tập trung trên Redis, bốn chiều, có chiều xuyên-hãng | S-01, S-02, S-03 |
| **AD-20** | Ghi vết chống chối bỏ: ghi **DB vật lý đo tại kết nối**, bất biến "tenant ↔ DB" có cảnh báo, kho chỉ-ghi-thêm tách riêng, phủ cả đọc vượt phạm vi và kết xuất lớn, danh sách truy vấn phát hiện + người trực | S-25, S-26, S-27, S-28 |
| **AD-21** | Biên trình duyệt: token trong bộ nhớ, refresh trong cookie `HttpOnly`, kiểm `Origin`, CORS đóng, CSP không `unsafe-inline`, allow-list giá trị thương hiệu tenant, SRI cho CDN | S-32 |
| **AD-22** | Kho quan trắc mang phân loại cao nhất: allow-list trường, che ở tầng sink, nhãn tenant, kiểm soát và ghi vết việc đọc log, thời hạn lưu | S-22, S-31 |
| **AD-23** | Sao lưu/khôi phục và luồng dữ liệu giữa môi trường: backup mã hoá khoá tách, đường lưu riêng theo hãng, kiểm chéo khi khôi phục, **cấm dữ liệu production xuống dev** | S-33 |
| **AD-24** | Kiểm chứng an ninh: bộ test rò rỉ hai tenant trên bốn khe (viết trước tính năng đầu tiên), SAST/DAST/quét bí mật/quét phụ thuộc, pen test trước hãng đầu tiên và trước hãng thứ ba | S-34 |
| **AD-15 (viết lại)** | Bảng một-một: mỗi điều "cấm" ⇄ đúng một cơ chế ép có tên. Luật meta: không thêm điều cấm mới nếu không nêu được cơ chế ép | S-30 |

---

# Ba việc nên làm trước mọi việc khác

1. **Xác minh và vá `ChangeCompany` trên production của web cũ.** Nó độc lập với WEB2, có thể đang mở, và mất chưa tới một ngày. (S-36)
2. **Chốt "mỗi hãng một login SQL riêng"** trước khi viết dòng code truy cập dữ liệu đầu tiên. Đây là thay đổi đổi **hậu quả** của mọi lỗi khác trong tài liệu này, và về sau rất khó lắp thêm. (S-12)
3. **Viết bộ test rò rỉ hai tenant** (HTTP · SignalR · cache · DB) trước tính năng đầu tiên, đúng như ADR-002 đã yêu cầu và bộ khung đã bỏ sót. Nó là thứ duy nhất biến các AD từ ý định thành ràng buộc. (S-34)
