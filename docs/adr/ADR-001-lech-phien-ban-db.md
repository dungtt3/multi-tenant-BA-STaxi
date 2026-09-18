# ADR-001 — Xử lý lệch phiên bản giữa API mới và DB khách hàng

- **Trạng thái:** Đề xuất (chưa chốt)
- **Ngày:** 2026-09-17
- **Liên quan:** D2 (một deployment, nhiều khách, DB tách riêng), D3 (BE → API service + gateway)
- **Kèm theo:** [ADR-003](ADR-003-chien-luoc-schema-18-db.md)

---

## Bối cảnh

Quyết định D2/D3 tạo ra một tình huống không tồn tại ở mô hình hiện tại: **một bản API phục vụ 18 database có thể đang ở 18 mức schema khác nhau.** Hôm nay mỗi khách một deployment nên web và DB luôn đi cùng nhau; sau D2 thì không.

Nếu không có cơ chế, *"web mới đã lên, DB khách còn cũ"* chỉ lộ ra khi một truy vấn gãy — giữa giờ cao điểm, trên màn hình của khách.

### Đã đo được

| Dữ kiện | Số đo |
|---|---|
| Số DB khách hàng | **18** cặp Database/Server (`Connection.config`) |
| `[Version.ApiSupported]` | **Đã tồn tại trong DB**: `Id, ApiGroup, Version, IsEnabled, FromDate, ToDate` — 2 dòng |
| `[Admin.DbMigrations]` | **Đã tồn tại trong DB**: `ScriptName, Checksum, ExecutedAt, Status, ErrorMessage` — 30 dòng |
| Tham chiếu tới hai bảng trên trong repo này | **Không có dòng nào** |

> **Đây là phát hiện quan trọng nhất của ADR này.** Cơ chế khai báo phiên bản API theo từng DB **đã có sẵn** và đang được dùng ở mức tối thiểu (2 dòng). Nó gần như chắc chắn thuộc về một hệ thống khác (repo backend/API, hoặc công cụ của DBA). Ta không cần phát minh — ta cần **tìm chủ sở hữu, rồi nhận lại và mở rộng**.

---

## Yếu tố quyết định

1. Sai lệch phải **lộ ra lúc khởi động hoặc lúc định tuyến**, không phải lúc truy vấn gãy.
2. Một khách chưa nâng cấp DB **không được làm sập** khách đã nâng cấp.
3. Thông điệp lỗi phải nói được *khách nào, thiếu phiên bản nào* — không phải stack trace.
4. Không thêm một nguồn sự thật thứ hai bên cạnh `[Version.ApiSupported]` đang có.

---

## Các phương án

### A. Gateway đọc `[Version.ApiSupported]` của từng tenant ⭐ *(đề xuất)*

Gateway phân giải tenant → đọc dải phiên bản API mà DB đó khai báo → định tuyến tới build API tương thích, hoặc trả lỗi rõ ràng.

- ✅ Dùng lại cơ chế đã có trong DB, không đẻ thêm khái niệm
- ✅ Sai lệch lộ ra **trước** khi chạm dữ liệu
- ✅ Mỗi khách nâng cấp theo nhịp riêng — đúng tinh thần D7 (thí điểm trước)
- ⚠️ Gateway trở nên có trạng thái về metadata tenant → cần cache riêng + cơ chế vô hiệu (dính vào khe `CacheKey`)
- ⚠️ Có thể phải chạy song song nhiều build API cùng lúc

### B. Phiên bản trong URL/header do client chọn (`/v1/`, `/v2/`)

- ✅ Chuẩn mực, ai cũng hiểu
- ❌ **Không giải bài toán này**: client đâu biết DB của khách đang ở mức nào. Giải lệch giữa *client và API*, không giải lệch giữa *API và DB*

### C. API tự dò schema lúc khởi động (introspection)

- ✅ Không cần bảng khai báo
- ❌ Mong manh: dò được cột tồn tại, không dò được *ngữ nghĩa* đã đổi
- ❌ 565 bảng / 10100 cột → dò đầy đủ là chậm và giòn

### D. Lockstep — bắt cả 18 DB cùng mức trước khi deploy

- ✅ Đơn giản nhất, không cần cơ chế gì
- ❌ **Giết đúng lợi ích của D2**: quay lại cảnh muốn ra một tính năng phải nâng cấp 18 khách cùng lúc
- ❌ Khách lớn luôn là người chặn lịch

---

## Đề xuất

**A làm xương sống, B làm lớp ngoài cho client.** Cụ thể:

1. **Điều kiện tiên quyết** — xác định ai sở hữu `[Version.ApiSupported]` và `[Admin.DbMigrations]`. Chưa trả lời được thì chưa được dựa vào chúng.
2. Gateway phân giải tenant → tra `ApiGroup` + dải `Version` (`IsEnabled`, `FromDate`, `ToDate`) của DB tenant đó, **cache có tenant trong key**, vô hiệu bằng tem phiên bản.
3. API khai báo tĩnh dải schema nó hỗ trợ; **kiểm tra lúc khởi động** và từ chối phục vụ tenant ngoài dải.
4. Tenant ngoài dải → **HTTP 409/503 với thông điệp có tên khách và phiên bản thiếu**, không phải truy vấn gãy. (Web cũ trả HTTP 200 kèm trang lỗi — bản mới không lặp lại.)
5. Mức schema thực tế suy ra từ `[Admin.DbMigrations]` (script cuối cùng `Status = thành công`), đọc lúc khởi động.

---

## Hệ quả

- Gateway giữ metadata tenant → **phải có chiến lược cache riêng ngay từ đầu**, không gắn sau.
- Có thể tồn tại nhiều build API song song → cần quy ước đặt tên và vòng đời khai tử phiên bản.
- Web cũ cũng nên đọc cùng nguồn phiên bản này, nếu không hai bản sẽ hiểu khác nhau về cùng một DB.
- `[Version.ApiSupported]` mới có 2 dòng: đang bị dùng dưới công suất, cần thiết kế lại ngữ nghĩa `ApiGroup` cho phạm vi rộng hơn.

## Việc còn mở

- [ ] Ai sở hữu hai bảng đó? Repo nào ghi vào chúng?
- [ ] `ApiGroup` hiện mang nghĩa gì (2 dòng đang là gì)?
- [ ] Dải phiên bản tính theo API hay theo schema — hay cả hai, và ánh xạ ra sao?
