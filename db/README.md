# `db/` — sổ hạng tenant và schema

## `tenancy-classes.json` — sổ hạng tenant (`AD-5`)

Mỗi bảng thuộc đúng **một** hạng: `Global` · `PerTenant` · `PerCompany` · `CrossLink`.
`AGENTS.md` nói thẳng: **chưa phân hạng ⇒ build đỏ**. Spine nói thẳng hơn: phân hạng là *điều kiện tiên
quyết, không phải việc dọn dẹp*.

### Trạng thái hiện tại

| | |
|---|---|
| Số bảng trong sổ | **279** |
| Đã duyệt | **0** |
| Chờ duyệt | **279** |
| Suy ra `PerCompany` | 164 |
| Suy ra `PerTenant` | 115 |
| Mục có cảnh báo | 120 |

**Không mục nào ở trạng thái `daDuyet`, và đó là đúng.** Hạng trong file này được **suy ra từ chứng cứ**,
không phải phán quyết. `AD-5` đòi một phán quyết của con người. Máy chỉ có thể nói *"bảng này có cột khoá
công ty"*; nó không thể nói *"bảng này thuộc về một công ty"* — hai điều đó không giống nhau.

### Sổ này được rút ra từ đâu

Từ thuộc tính `[Table("...")]` trong `ba_staxi_webadmin/BA.STaxi.Data/Models/*.cs`, ngày 2026-09-18.
Đó là **tập bảng mà mã nguồn web cũ chạm tới** — không phải toàn bộ schema. Spine ghi DB đang chạy có
**565 bảng**, nên còn khoảng **286 bảng** chưa có mặt ở đây.

### Phát hiện quan trọng nhất: khoá công ty có **bốn** cách viết

```
CompanyId      CompanyID      FK_CompanyID      FK_CompanyId
```

Đây không phải chuyện thẩm mỹ. Bản rút đầu tiên chỉ tìm đúng `CompanyId` và cho ra 126 bảng
`PerCompany`. Bản đúng cho ra **164** — tức **38 bảng suýt bị xếp nhầm thành `PerTenant`**, trong đó có
`[Cust.Books]` (`FK_CompanyId`) và `[Comp.Cars]` (`CompanyID`).

Xếp nhầm theo hướng đó là hướng nguy hiểm: `PerTenant` nghĩa là *"database đã là biên — **cấm** lọc
`CompanyId`"*. Một repository tuân thủ đúng hạng sai ấy sẽ trả về **dữ liệu của mọi công ty trong hãng**,
và nó làm vậy một cách hoàn toàn "đúng luật".

**Hệ quả cho người viết repository:** không được giả định tên cột là `CompanyId`.
`TenantScopedRepository.QueryPerCompanyAsync` đòi SQL có tham số `@CompanyId`, nhưng **tên cột** trong
mệnh đề `WHERE` phải tra từ sổ này.

### Chính sách duyệt: **theo nhu cầu**

Quyết định của chủ sản phẩm, 2026-09-18: **không duyệt hàng loạt**. Mỗi màn hình duyệt vài bảng mà nó
chạm tới, ngay trong cùng thay đổi đang làm.

Đổi lại, việc duyệt rải đều suốt dự án thay vì dồn một buổi — và `R14` là thứ nhắc bạn đúng lúc cần nhắc,
không sớm hơn.

#### Luồng thực tế

1. Bạn viết repository cho màn hình mới, có câu SQL chạm bảng `[X]`.
2. Build đỏ ở `R14`, thông điệp nói rõ bảng nào và đang ở trạng thái gì.
3. Bạn duyệt đúng bảng đó (bên dưới), rồi build lại.

**Đừng duyệt trước cho những bảng chưa dùng tới.** Phán quyết mà không có màn hình cụ thể trong đầu là
phán quyết dựa trên phỏng đoán, tức đúng thứ trạng thái `chuaDuyet` đang ghi nhận.

### Cách duyệt một bảng

1. Đọc `chungCu` và `canhBao` của mục đó.
2. Quyết định hạng thật. Bốn câu hỏi, theo thứ tự:
   - Dữ liệu này **giống nhau ở mọi hãng** và có nơi ở vật lý xác định? ⇒ `Global` (hiếm — xem `AD-6`)
   - Thuộc về hãng nhưng **không** chia theo công ty? ⇒ `PerTenant`
   - Thuộc về **một** công ty? ⇒ `PerCompany`
   - **Nối** giữa các công ty trong cùng hãng? ⇒ `CrossLink`
3. Nếu khoá công ty là `nullable`, trả lời trước: **dòng có giá trị NULL thuộc về ai?** Chưa trả lời được
   thì chưa duyệt được — 85 mục trong sổ đang vướng câu này.
4. Sửa `hang` nếu cần, đổi `trangThai` thành `daDuyet`, và **viết lại `chungCu` thành lý do thật** của
   phán quyết, thay cho câu suy ra tự động.
5. Cập nhật `thongKe`. Không cần đếm tay: chạy `R12`, nó sẽ đỏ và **nói thẳng con số đúng** —
   `thongKe.daDuyet = 0 nhưng đếm thật được 1`.

Đổi trạng thái là một thay đổi **có review**. Cái giá của một phán quyết sai là rò rỉ dữ liệu giữa các
công ty, và nó không để lại lỗi nào để ai phát hiện.

### Ba giới hạn đã biết của bản rút tự động

- Chỉ phủ 279/565 bảng.
- **Không** phát hiện được ứng viên `CrossLink` khi bảng đã có khoá sở hữu. Các cột như `CompanyIDOrg`,
  `PaymentCompanyId`, `PromoteCompanyIds` trỏ tới công ty **khác** — chỉ người duyệt phân biệt được.
- **Không** phân biệt được `Global` với `PerTenant`: cả hai đều biểu hiện là "không có khoá công ty".
  Mặc định gán `PerTenant`; người duyệt phải nâng lên `Global` khi đúng.

### Việc còn thiếu

- 286 bảng chưa có trong sổ — cần danh sách bảng đầy đủ từ DBA hoặc một lần đọc `sys.tables`.
- `screen-owners.json` (`AD-20`) — sổ chủ sở hữu màn hình, chưa dựng.
- Thư mục `up/`, `views/`, `sprocs/`, `functions/` của grate (`AD-10`) — chưa dựng.
