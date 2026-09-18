# ADR-003 — Chiến lược schema cho 18 database khách hàng

- **Trạng thái:** Đề xuất (chưa chốt)
- **Ngày:** 2026-09-17
- **Liên quan:** D2 (DB vẫn tách riêng từng khách), D3 (BE → API service)
- **Kèm theo:** [ADR-001](ADR-001-lech-phien-ban-db.md)

---

## Bối cảnh

D2 giữ nguyên mô hình mỗi khách một DB. Nghĩa là **mọi thay đổi schema vẫn nhân lên 18 lần** — và repo này không có công cụ nào cho việc đó.

### Đã đo được

**Trên DB đang hoạt động (`G7` @ `<may-chu-noi-bo>`):**

| Hạng mục | Số đo |
|---|---|
| Bảng | **565** |
| Cột | **10.100** |
| Stored procedure | **2.003** |
| Bảng có cột `CompanyId` | **215 / 565** (~38%) → **350 bảng chưa rõ hạng tenant** |

**Trong repo:**

| Hạng mục | Hiện trạng |
|---|---|
| File `.sql` dưới `tasks/` | **0** |
| EF6 migrations | Không có, initializer đã tắt (`AGENTS.md`) |
| Công cụ chạy DDL | Không có trong repo |

**Trong DB, nhưng không có trong repo:**

> `[Admin.DbMigrations]` — `ScriptName, Checksum, ExecutedAt, Status, ErrorMessage`, **30 dòng**.
>
> Đây là một sổ cái migration đúng nghĩa, **có checksum**. Ai đó đã xây một bộ chạy script tử tế — nhưng **không một dòng nào trong repo này tham chiếu đến nó**. Công cụ sống ở nơi khác.

**Hệ quả thẳng thắn:** hiện tại **không ai biết 18 DB đang lệch nhau bao nhiêu.** DDL nằm ngoài quản lý phiên bản của repo này, và không có bài kiểm nào so sánh chúng.

---

## Yếu tố quyết định

1. Một thay đổi schema phải chạy được lên N DB **theo đúng thứ tự**, và biết được DB nào đã chạy tới đâu.
2. Trôi schema phải **phát hiện được**, không chờ tới lúc truy vấn gãy (ADR-001 phụ thuộc điều này).
3. Phải có lợi **ngay cho web cũ**, không chờ web mới — đây là lý do việc này đứng trước trong thứ tự ưu tiên.
4. 2.003 stored procedure không thể bị bỏ qua: chúng là một phần của schema, và là nơi phần lớn nghiệp vụ đang sống.
5. **Trong suốt giai đoạn song song, hai ứng dụng cùng ghi vào một database** (xem [ADR-004](ADR-004-mot-domain-dinh-danh-va-topo.md)) — ràng buộc này chặt hơn mọi thứ khác ở đây.

### ⚠️ Luật schema cho giai đoạn hai ứng dụng chung một DB

WEB2 là trang web riêng nhưng **dùng chung database với web cũ**. Nghĩa là mọi thay đổi schema phải **tương thích ngược với web cũ**, cho tới khi hãng đó ngừng dùng web cũ hẳn.

| | |
|---|---|
| ✅ An toàn | Thêm cột `NULL`-able · thêm bảng mới · thêm index · thêm proc mới |
| ⛔ **Cấm** khi hãng còn dùng cả hai | Đổi tên cột · đổi kiểu dữ liệu · xoá cột · đổi nghĩa của giá trị · sửa proc web cũ đang gọi theo cách đổi hình dạng kết quả |
| ⚠️ Cẩn trọng | Một bảng, **một người ghi**. Nếu buộc phải hai, viết hợp đồng tường minh + test tích hợp hai chiều |

Đây là cái giá của việc chạy song song an toàn, và nó phải được viết thành luật cho cả đội — không phải để mỗi người tự suy ra.

---

## Các phương án

### A. Nhận lại `[Admin.DbMigrations]` và đưa bộ chạy script vào repo ⭐ *(đề xuất)*

Script `.sql` có đánh số, nằm trong git, chạy tuần tự, ghi sổ + checksum, dừng ngay khi lỗi.

- ✅ **Sổ cái đã tồn tại và đang được dùng** — nhận lại rẻ hơn thay thế
- ✅ Checksum bắt được trường hợp ai đó sửa script đã chạy
- ✅ DDL vào git → có review, có lịch sử, có nguồn sự thật
- ⚠️ Phải tìm chủ sở hữu hiện tại trước, nếu không sẽ có hai bộ cùng ghi vào một bảng

### B. Công cụ có sẵn — DbUp / Flyway / RoundhousE

- ✅ Chính xác hình dạng của A, khỏi tự viết
- ✅ DbUp là .NET, hợp với D1
- ⚠️ Có bảng sổ cái riêng → phải hoặc di trú `[Admin.DbMigrations]` sang, hoặc cấu hình cho nó dùng đúng bảng đó
- ⚠️ Thêm một phụ thuộc mà đội phải học

### C. EF Core migrations cho web mới, DB cũ để yên

- ✅ Đúng chuẩn của .NET 8
- ❌ Không dùng được với 565 bảng / 2.003 proc đang tồn tại nếu không scaffold toàn bộ
- ❌ Không giải bài toán 18 DB — chỉ giải cho bảng mới
- ⚠️ Có thể dùng **hạn chế** cho các bảng hoàn toàn mới của nền tảng (`Admin.SchemaVersion`, bảng quyền…)

### D. Giám sát trôi schema (schema diff) giữa 18 DB

- ✅ Trả lời câu hỏi *"ta đang ở đâu"* — câu hỏi hiện chưa ai trả lời được
- ✅ Làm được **ngay hôm nay**, chỉ cần quyền đọc
- ❌ Chỉ phát hiện, không sửa → là bạn đồng hành của A/B, không thay thế

---

## Đề xuất

**A (hoặc B = DbUp cấu hình để ghi vào đúng `[Admin.DbMigrations]`), cộng D làm job giám sát.** Theo thứ tự:

1. **Kiểm kê trôi schema trước tiên.** Một truy vấn chỉ-đọc so sánh `sys.tables` / `sys.columns` / `sys.procedures` giữa 18 DB, xuất ra bảng khác biệt. Rẻ, làm được ngay, và nó quyết định mọi thứ phía sau. Hiện không ai biết con số này.
2. **Tìm chủ sở hữu `[Admin.DbMigrations]`** — repo nào ghi vào nó, ai chạy, quy trình ra sao.
3. **Đưa DDL vào git**: `db/migrations/{số}-{mô-tả}.sql`, chạy tuần tự, ghi sổ, dừng khi lỗi. Theo `AGENTS.md`, agent viết script vào `tasks/` cho người chạy — quy tắc đó vẫn giữ, chỉ là script giờ có nhà.
4. **Phân loại 350 bảng chưa rõ hạng tenant.** Đây chính là *nước đi đầu tiên* của kế hoạch: một test đang đỏ khẳng định mọi thực thể đều được phân loại (toàn cục / theo tenant / liên kết chéo).
5. **Chốt số phận 2.003 stored procedure** — xem phần dưới.

---

## Câu hỏi lớn nhất: 2.003 stored procedure

Con số này chi phối cả D3 lẫn ADR này. Quyết định D3 (backend → API service) phải chọn:

- **API bọc procs** — nghiệp vụ ở lại DB. Ra nhanh, nhưng schema của 18 DB buộc phải đồng bộ chặt, và ADR-001 khó hơn nhiều vì "phiên bản API" thực chất là "phiên bản proc".
- **API thay procs** — nghiệp vụ chuyển lên tầng .NET. Đúng hướng dài hạn và làm ADR-001 dễ thở, nhưng là khối lượng công việc lớn nhất của cả dự án.
- **Lai** — phạm vi đợt đầu (Dashboard, Online, danh mục, CRUD config) chuyển lên .NET; báo cáo nặng để lại proc.

**Lai gần như chắc chắn là câu trả lời**, nhưng phải chốt tường minh và viết ra đường biên — nếu không, mỗi lập trình viên sẽ tự quyết theo từng màn hình, và sau một năm không ai nói được nghiệp vụ đang nằm ở đâu.

---

## Hệ quả

- DDL vào git → thay đổi thói quen làm việc của cả đội và của DBA. Cần thống nhất trước, không áp đặt.
- Job giám sát trôi schema cần quyền đọc tới cả 18 DB — hiện `Connection.config` chỉ bật một dòng mỗi lần, nên cần một nguồn danh sách kết nối riêng cho công cụ.
- Việc phân loại 350 bảng sẽ lộ ra những bảng không ai còn nhớ dùng làm gì. Đó là kết quả có giá trị, không phải trở ngại.

## Việc còn mở

- [ ] 18 DB đang lệch nhau bao nhiêu? (chạy kiểm kê ở bước 1)
- [ ] Ai sở hữu `[Admin.DbMigrations]` và 30 dòng trong đó là những script nào?
- [ ] Trong 2.003 proc, bao nhiêu cái thực sự còn được gọi? (bật kiểm toán một tuần là biết)
- [ ] Đường biên "proc hay .NET" đặt ở đâu, và ai gác nó?
