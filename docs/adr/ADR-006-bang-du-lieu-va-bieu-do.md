# ADR-006 — Bảng dữ liệu và biểu đồ

- **Trạng thái:** Chốt
- **Ngày:** 2026-09-18
- **Liên quan:** `ADR-005` (react-bootstrap), `docs/design-guide.md`, `AD-1` (FE build tĩnh), `AD-4` (phạm vi chỉ từ token đã ký), `AD-12` (BFF gom dữ liệu)

---

## Quyết định

| | Chọn | Thay cho |
|---|---|---|
| Bảng dữ liệu | **AG Grid** — như web 1 | `<Table>` trần của react-bootstrap |
| Biểu đồ | **Apache ECharts** | c3 / flot / morris của bộ mẫu INSPINIA (đều là jQuery) |

---

## Bối cảnh

`ADR-005` chốt `react-bootstrap`, nhưng nó chỉ có `<Table>` trần. Màn hình danh sách của hệ này —
báo cáo doanh thu, nhật ký API, lịch sử thẻ, giám sát online — cần sắp xếp, lọc, phân trang phía máy chủ,
và cột co giãn. Bộ mẫu INSPINIA có sẵn biểu đồ, nhưng cả ba thư viện của nó đều là jQuery nên `AD-1` loại.

---

## Đã đo được

### Web 1 đang dùng gì

| Dữ kiện | Giá trị |
|---|---|
| Gói | `@ag-grid-enterprise/all-modules` |
| Bản | **v30.2.1** |
| Giấy phép khai trong file | `@license Commercial` |
| Theme | `ag-theme-quartz.css` |
| Nạp ở | `App_Start/BundleConfig.cs`, cùng `locale.min.js` và ba file bọc tự viết |
| Màn hình dùng | Report (7 màn), `Trace/OnlineNew`, `ApiLog`, `Dashboard/HeatMapIndex`, `Card/CardTransaction`, `Customer/CustSmsOtpType`, … |

Hai tính năng **chỉ có ở bản Enterprise** đang được dùng thật, tìm thấy trong `AgGridSvSide.js`:

- **`serverSide`** — Server-Side Row Model. Đây không phải tiện nghi: nó là lý do một lưới chạy được trên
  bảng hàng triệu dòng mà không kéo hết về trình duyệt.
- **`sideBar`** — Tool Panel bên phải.

Còn có `AgGridSvSideGroup.js`, tức server-side kèm gom nhóm — cũng Enterprise.

### Phiên bản hiện tại — kiểm chứng trên npm ngày 2026-09-18

| Gói | Bản | Peer |
|---|---|---|
| `ag-grid-community` | 36.2.0 | — |
| `ag-grid-enterprise` | 36.2.0 | — |
| `ag-grid-react` | 36.2.0 | `react ^16.8 \|\| ^17 \|\| ^18 \|\| ^19` — React 19.3.0 **được khai báo tường minh** |
| `echarts` | 6.1.0 | — |
| `echarts-for-react` | 3.0.6 | `react >= 16`, `echarts ^3 \|\| ^4 \|\| ^5 \|\| ^6` — còn được bảo trì, cập nhật 2026-05-19 |

---

## Bản quyền AG Grid Enterprise: làm tương tự web 1

**Quyết định của chủ sản phẩm, 2026-09-18: làm tương tự web 1** — dùng bản Enterprise, không nạp khoá.

Tìm `setLicenseKey` trong toàn bộ `BA.STaxi.Web` chỉ thấy trong chính file thư viện, không có lời gọi nào
trong mã ứng dụng. WEB2 làm giống vậy.

Đã kiểm tận nơi trong `ag-grid-enterprise@36.2.0` xem bản mới xử sự thế nào — web 1 đang ở v30, sáu bản
chính cách nhau nên không suy từ hành vi cũ ra được. Chuỗi lấy thẳng từ mã nguồn gói:

```
**  AG Grid Enterprise License  **
**    License Key Not Found     **
* All AG Grid Enterprise features are unlocked for trial.
* If you want to hide the watermark please email info@ag-grid.com...
```

Kèm một phần tử watermark hiện trên lưới (`.ag-watermark`, `.ag-watermark-text`, font Impact 19px).

Nghĩa là ở v36 hành vi vẫn **giống v30**:

| | |
|---|---|
| Tính năng | **Không bị chặn** — `serverSide`, `sideBar` chạy đủ |
| Console | in bảng thông báo mỗi lần khởi tạo lưới |
| Giao diện | **watermark hiện trên lưới, ở mọi môi trường** |

Một hệ quả nên biết trước chứ không nên gặp lúc hãng thí điểm chạy: watermark là thứ **người dùng cuối
nhìn thấy**, không phải chỉ phiền lúc phát triển. 18 hãng sẽ thấy nó trên mọi màn hình có lưới. Web 1 đã
như vậy, nên đây không phải thay đổi — chỉ là điều cần nói rõ một lần.

Chính AG Grid gọi trạng thái này là *trial*. Đây là quyết định thương mại của chủ sản phẩm, ghi lại ở đây
để về sau không ai phải đoán nó là cố ý hay bỏ sót.

**Hệ quả kỹ thuật:** không có khoá thì không có bí mật nào phải quản, nên `AD-22` không bị đụng tới. Nếu
sau này mua giấy phép, khoá nạp qua biến môi trường lúc build và **không commit vào git**.

Bản Community (MIT) đủ cho: sắp xếp, lọc, phân trang phía client, đổi kích thước và thứ tự cột, cell
renderer, xuất CSV. **Không** đủ cho `serverSide` và `sideBar` — hai thứ web 1 đang dùng — nên hạ xuống
Community không phải một lựa chọn tương đương.

---

## Chỗ AG Grid gặp luật cách ly tenant — đọc trước khi nối API

Server-Side Row Model hoạt động bằng cách **chính lưới tự phát yêu cầu dữ liệu**, kèm tham số sắp xếp,
lọc, gom nhóm, khoảng dòng. Tức là một khối JSON do **client** dựng, đi thẳng vào API.

Ba luật áp vào đây, không có ngoại lệ:

1. **`AD-4`** — khối tham số đó **không bao giờ** được mang `tenantCode` hay `companyId`, dù đặt tên gì.
   Phạm vi lấy từ token. `ScopeParameterGuardMiddleware` đã chặn theo tên, nhưng bộ lọc của AG Grid là
   cấu trúc lồng nhau nên lưới chặn theo tên **không với tới đáy** — phần kiểm phải nằm ở chỗ dựng câu
   truy vấn.
2. **`AD-5`** — tên cột và toán tử lọc do client gửi là dữ liệu client gửi. Phải đối chiếu với một danh
   sách trắng cột cho phép, không ghép thẳng vào SQL. Đây cũng là chỗ `AD-9` cấm dynamic SQL.
3. **`AD-18`** — kết xuất lớn phải để lại vết. Một lưới cho phép kéo 50.000 dòng là một đường rò dữ liệu
   hợp lệ về mặt kỹ thuật.

Khi dựng màn hình danh sách đầu tiên, **bộ test rò rỉ hai tenant phải mọc thêm bài**: gọi endpoint của
lưới bằng token hãng A, kèm bộ lọc cố trỏ sang dữ liệu hãng B (`AD-24` bắt mở rộng mỗi khi thêm một cơ
chế mới).

---

## ECharts

Chọn vì: bộ mẫu INSPINIA dùng c3/flot/morris — cả ba đều jQuery nên `AD-1` loại; ECharts không phụ thuộc
khung, có sẵn bản đồ và heatmap (web 1 có `Dashboard/HeatMapIndex`), và chạy tốt với dữ liệu lớn.

Hai điều phải xử lý ngay từ component đầu tiên:

- **ECharts không tự co giãn.** Phải gọi `resize()` khi vùng chứa đổi kích thước. Bắt `window.resize` là
  chưa đủ — sidebar thu gọn hay panel đóng/mở cũng đổi kích thước vùng chứa mà cửa sổ không đổi. Dùng
  `ResizeObserver` trên chính vùng chứa. Với ưu tiên responsive đã chốt, đây là lỗi sẽ gặp ngay ở màn
  hình đầu tiên nếu bỏ qua.
- **Vùng chứa phải có chiều cao tường minh.** ECharts không suy ra chiều cao từ nội dung; chiều cao 0 thì
  biểu đồ không hiện và không báo lỗi gì.
- Nhập theo kiểu tree-shaking (`echarts/core` cộng từng biểu đồ, từng component) thay vì nhập cả gói.
  18 hãng chạy trên mạng nội bộ, gói đầy đủ là hơn một megabyte không cần thiết.

`echarts-for-react` 3.0.6 là lớp bọc mỏng, còn được bảo trì. Dùng nó, nhưng `ResizeObserver` vẫn phải tự
gắn — lớp bọc chỉ theo dõi cửa sổ.

---

## Bảng trên màn hình hẹp: chỉ dashboard và CRUD

Quyết định của chủ sản phẩm, 2026-09-18:

| Loại màn hình | Màn hình hẹp |
|---|---|
| **Báo cáo** | **Không áp dụng.** Giữ lưới đầy đủ, cuộn ngang. Báo cáo là việc của máy để bàn |
| **Dashboard** | Có — thẻ xếp một cột, biểu đồ co theo vùng chứa |
| **CRUD** | Có — lưới chuyển sang **danh sách thẻ** dưới `md` |

Chọn danh sách thẻ (không phải cuộn ngang ghim cột đầu) cho dashboard và CRUD, vì màn hình CRUD thường
ít cột và thiên về hành động — mỗi dòng thành một thẻ hiện 3–4 trường quan trọng cộng nút sửa/xoá thì đọc
được bằng ngón tay cái. Cuộn ngang giữ lại cho báo cáo, nơi số cột mới là thứ người dùng cần.

Lợi ích đi kèm: không phải làm responsive cho bảy màn hình báo cáo — phần tốn công nhất và ít người mở
trên điện thoại nhất.

---

## Hệ quả

- Ghim `ag-grid-react`, `ag-grid-community` và `ag-grid-enterprise` ở **36.2.0**; `echarts` 6.1.0;
  `echarts-for-react` 3.0.6. Ba gói AG Grid phải cùng một số bản — lệch bản giữa chúng là lỗi lúc chạy.
- Web 1 đang ở AG Grid **v30**, WEB2 sẽ ở **v36**. Đừng bê cấu hình lưới từ web 1 sang — sáu bản chính
  cách nhau, API cấu hình cột và theme đã đổi. Lấy **hình dạng màn hình**, không lấy cấu hình.
- **Theme của lưới phải ánh xạ sang token** trong `docs/design-guide.md` (`#337ab7`, viền `#e7eaec`, chữ
  14px trong bảng). AG Grid các bản gần đây chuyển sang Theming API bằng đối tượng theme thay cho file CSS
  — cần kiểm cách làm đúng ở 36.2.0 khi dựng, đừng chép lối nạp `ag-theme-quartz.css` của web 1.
- Ba dòng phiên bản này **chưa vào bảng Stack của spine**; theo `AGENTS.md` phải qua
  `docs/.architecture-memlog.md` trước.

---

## Việc còn mở

- [ ] Danh sách trắng cột cho phép sắp xếp/lọc — đặt ở đâu, ai giữ (liên quan `AD-5`, `AD-9`)
- [ ] Ngưỡng số dòng của một lần kết xuất, và ngưỡng nào thì bắt ghi vết (`AD-18`)
- [ ] Ngưỡng độ rộng chuyển sang danh sách thẻ, và chọn 3–4 trường nào cho mỗi màn hình CRUD
