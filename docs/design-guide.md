# Design guide — WEB2

Nguồn tham chiếu hình dạng: `C:\Users\dungtt3\Files\HTML5_Full` — **INSPINIA Responsive Admin Theme**,
216 trang HTML mẫu, 81 CSS, 853 ảnh.

Ba quyết định dưới đây là của chủ sản phẩm, chốt ngày 2026-09-18, và **ghi đè** phần tương ứng của bộ mẫu.

| | Chốt | Thay cho |
|---|---|---|
| Màu chủ đạo | `#337ab7` | `#1ab394` của INSPINIA |
| Cỡ chữ | chuẩn hôm nay, gốc `16px` | `13px` |
| Khung CSS | **Bootstrap 5.3.8** | Bootstrap 4.1.0 của bộ mẫu |

---

## Vì sao ba quyết định này không phá tính liền mạch

Web cũ **không** thuần INSPINIA. Đếm trong `ba_staxi_webadmin`:

| Màu | Số lần xuất hiện | Từ đâu |
|---|---|---|
| `#337ab7` | **722** | primary mặc định của Bootstrap 3 |
| `#e7eaec` | 351 | INSPINIA |
| `#1ab394` | 314 | INSPINIA |
| `#2f4050` | 39 | INSPINIA |

`BA.STaxi.Web` là hỗn hợp: một số màn hình dùng theme INSPINIA, phần lớn còn lại dùng Bootstrap 3 thuần
(có `Bootstrap v3.1.0`, `v3.3.7`, `v3.4.1` cùng tồn tại trong repo). **Màu xanh dương `#337ab7` mới là
màu phổ biến nhất trong hệ cũ**, không phải màu xanh lá.

Nên chọn `#337ab7` không phải là rời bỏ ngôn ngữ thị giác cũ — nó là chọn đúng cái phổ biến hơn.

Hai lợi ích đi kèm, cả hai đều đo được:

**1. Hết chồng nghĩa màu.** Ở INSPINIA, `#1ab394` vừa là màu thương hiệu vừa là màu "thành công", nên
một nút chính nằm cạnh một badge trạng thái thì người dùng không phân biệt được đâu là hành động, đâu là
tình trạng. Ở màn hình điều xe — nơi trạng thái chuyến đi là thông tin sống còn — đó là lỗi thiết kế
thật. Chuyển primary sang xanh dương thì xanh lá trở lại đúng nghĩa duy nhất: **thành công**.

**2. Độ tương phản đạt chuẩn.** Đo theo WCAG 2.1:

| Tổ hợp | Tỉ lệ | AA cho chữ thường (≥ 4.5:1) |
|---|---|---|
| Trắng trên `#1ab394` | 2.65:1 | ❌ trượt |
| Trắng trên `#337ab7` | 4.56:1 | ✅ đạt |
| `#337ab7` trên trắng | 4.56:1 | ✅ đạt |
| `#337ab7` trên nền xám `#f3f3f4` | 4.11:1 | ❌ trượt |
| `#286090` trên nền xám `#f3f3f4` | 5.98:1 | ✅ đạt |

> **Luật rút ra, áp dụng ngay:** `#337ab7` dùng làm **nền** (nút, thanh chọn). Làm **chữ hoặc link trên
> nền xám** thì dùng `#286090`. Bản gốc chỉ đạt 4.56:1 trên nền trắng — sát ngưỡng, nên tụt xuống dưới
> chuẩn ngay khi nền không còn trắng tinh.

---

## Luật nền: lấy thiết kế, không lấy code

| | |
|---|---|
| ✅ Lấy | bố cục màn hình · hình dạng component · thang khoảng cách · 216 trang mẫu làm tham chiếu thị giác |
| ⛔ Không lấy | file `.js` · markup jQuery · `metisMenu` · `bootstrap.js` · `.less`/`.scss` của theme · **bảng màu và cỡ chữ của theme** |

`AD-1` quy định FE là **Vite + React 19 + TypeScript, build ra file tĩnh**.

Với quyết định Bootstrap 5, luật này càng chặt hơn: bộ mẫu viết cho **Bootstrap 4**, và Bootstrap 5 đổi
tên hàng loạt class. Chép markup từ trang mẫu sang sẽ cho ra thứ trông gần đúng và hỏng ở chỗ không ai
ngờ. Mở trang mẫu để **nhìn**, rồi dựng lại bằng component của mình.

Bảng đổi tên hay gặp nhất khi đọc trang mẫu:

| Bootstrap 4 (trong bộ mẫu) | Bootstrap 5 |
|---|---|
| `ml-*` / `mr-*` | `ms-*` / `me-*` |
| `pl-*` / `pr-*` | `ps-*` / `pe-*` |
| `text-left` / `text-right` | `text-start` / `text-end` |
| `float-left` / `float-right` | `float-start` / `float-end` |
| `.form-group` | bỏ hẳn — dùng `mb-3` |
| `.custom-select` | `.form-select` |
| `.custom-control` / `.custom-checkbox` | `.form-check` |
| `.badge-primary` | `.text-bg-primary` |
| `.close` | `.btn-close` |
| `.sr-only` | `.visually-hidden` |
| `.font-weight-bold` | `.fw-bold` |
| `.no-gutters` | `.g-0` |
| `.jumbotron`, `.card-deck` | bỏ hẳn |

---

## Token

### Màu

| Vai trò | Mã | Ghi chú |
|---|---|---|
| **Chủ đạo** | `#337ab7` | nền nút chính, thanh chọn, trạng thái hoạt động |
| Chủ đạo — rê chuột | `#286090` | và là màu chữ/link an toàn trên nền xám |
| Chủ đạo — nhấn giữ | `#204d74` | viền khi active |
| Nền sidebar | `#2f4050` | giữ của INSPINIA |
| Sidebar rê chuột | `#293846` | |
| Chữ trong sidebar | `#a7b1c2` | mục đang chọn chuyển `#ffffff` |
| Nền vùng nội dung | `#f3f3f4` | |
| Nền card | `#ffffff` | |
| Viền / đường kẻ | `#e7eaec` | |
| Chữ thường | `#676a6c` | |
| Chữ mờ | `#999999` | chỉ dùng cho chú thích, không dùng cho nội dung |

Màu trạng thái — giữ nguyên của INSPINIA, nhưng nay **chỉ còn nghĩa trạng thái**:

| Trạng thái | Mã |
|---|---|
| Thành công | `#1ab394` |
| Thông tin | `#23c6c8` |
| Cảnh báo | `#f8ac59` |
| Nguy hiểm | `#ed5565` |

> Chữ trắng trên `#f8ac59` chỉ đạt khoảng 2:1. Badge cảnh báo phải dùng **chữ sẫm** trên nền vàng, đừng
> dùng chữ trắng.

### Chữ

```
font-family: "Open Sans", "Helvetica Neue", Helvetica, Arial, sans-serif;
font-size: 16px;
line-height: 1.5;
color: #676a6c;
```

Thang chữ cho ứng dụng quản trị — chặt hơn mặc định của Bootstrap 5, vì `h1` mặc định 2.5rem quá lớn cho
màn hình dày dữ liệu:

| Cấp | rem | px |
|---|---|---|
| `h1` — tiêu đề trang | 1.75 | 28 |
| `h2` | 1.5 | 24 |
| `h3` — tiêu đề card | 1.25 | 20 |
| `h4` | 1.125 | 18 |
| body | 1 | 16 |
| bảng, nhãn phụ, chú thích | 0.875 | 14 |

**Hệ quả phải xử lý, không phải chuyện thẩm mỹ:** 13px → 16px là mỗi ký tự rộng thêm khoảng 23%. Một
bảng điều xe đang vừa 12 cột ở web cũ sẽ chỉ còn vừa khoảng 10 cột ở cùng độ rộng màn hình. Ba cách giảm
nhẹ, dùng kết hợp:

1. Bảng dùng `0.875rem` (14px) — vẫn lớn hơn 13px cũ, nhưng không mất quá nhiều cột.
2. Dùng biến thể bảng nén (`.table-sm`) cho màn hình danh sách.
3. **Đo trước khi dựng.** Lấy màn hình dày cột nhất của web cũ, đếm số cột, thử ở 16px trên độ phân giải
   thật của máy điều phối viên. Phát hiện lúc dựng thì sửa được; phát hiện lúc hãng thí điểm chạy thì không.

**Tự host font, đừng gọi CDN** — 18 hãng chạy trên mạng nội bộ.

Lưu ý một chỗ dễ nhầm: bộ mẫu **không** kèm file Open Sans, nó nạp từ `fonts.googleapis.com`. Chỉ Font
Awesome và Glyphicons mới có sẵn file `.woff`/`.ttf` trong bộ. WEB2 tự host Open Sans bằng
`@fontsource/open-sans` 5.3.0, và **chỉ nạp subset `latin`, `latin-ext`, `vietnamese`** — nạp cả bộ thì
kéo theo cyrillic, greek, hebrew, gấp đôi kích thước tải mà không ai dùng.

Subset `vietnamese` là bắt buộc, không phải tuỳ chọn: thiếu nó thì chữ có dấu rơi về font dự phòng và
lệch hẳn khỏi phần còn lại của giao diện.

Glyphicons thì bỏ: Bootstrap 5 không còn dùng.

### Card — `.ibox`

Đơn vị bố cục cơ bản. Giữ nguyên hình dạng của INSPINIA:

```
khoảng cách giữa các card   25px
tiêu đề card                nền #ffffff · viền trên 2px #e7eaec · không viền dưới
thân card                   nền #ffffff · padding 15px 20px 20px 20px · viền trên-dưới 1px #e7eaec
```

Đặc điểm nhận dạng là **viền trên 2px**, không bo góc, không đổ bóng. Bỏ nó đi thì màn hình lạc khỏi hệ cũ
ngay cả khi màu đúng. Đây là component React đầu tiên cần dựng.

### Khoảng cách

| | |
|---|---|
| Giữa các card | `25px` |
| Trong card | `15px 20px 20px 20px` |
| Mục menu sidebar | `14px 20px 14px 25px` |
| Tiêu đề trang | `0 10px 20px 10px` |

---

## Bố cục

```
┌──────────────┬──────────────────────────────────────┐
│              │  thanh trên: breadcrumb, user, thoát │
│  sidebar     ├──────────────────────────────────────┤
│  #2f4050     │  tiêu đề trang                       │
│              │  ┌────────────────────────────────┐  │
│  menu nhiều  │  │ card .ibox                     │  │
│  cấp         │  └────────────────────────────────┘  │
│              │  nền #f3f3f4                         │
└──────────────┴──────────────────────────────────────┘
```

Sidebar thu gọn được thành dải icon. Giữ hành vi này — người dùng web cũ đã quen.

---

## Phiên bản — kiểm chứng trên npm ngày 2026-09-18

| Gói | Bản | Ghi chú |
|---|---|---|
| `bootstrap` | 5.3.8 | không cần jQuery; có sẵn CSS custom properties `--bs-*`; có chế độ màu qua `data-bs-theme` |
| `react-bootstrap` | 2.10.10 | chốt ở ADR-005; peer `react >= 16.14.0` nên React 19.3.0 thoả |
| `bootstrap-icons` | 1.13.1 | chỉ cần nếu không dùng Font Awesome có sẵn trong bộ mẫu |
| `ag-grid-react` | 36.2.0 | chốt ở ADR-006; peer khai báo React 19 tường minh |
| `echarts` | 6.1.0 | chốt ở ADR-006 |
| `echarts-for-react` | 3.0.6 | lớp bọc mỏng, còn bảo trì |

> **Chưa đưa vào bảng Stack của spine.** `AGENTS.md` quy định spine là luật và không sửa spine mà bỏ qua
> `docs/.architecture-memlog.md`. Ba dòng trên cần được ghi vào memlog rồi mới thêm vào bảng Stack — là
> một việc riêng, có review.

---

## Trang mẫu đáng xem trước khi dựng màn hình

| Cần dựng gì | Mở trang nào |
|---|---|
| Bảng danh mục, CRUD config | `table_basic.html`, `table_data_tables.html` |
| Biểu mẫu | `form_basic.html`, `form_advanced.html`, `form_wizard.html` |
| Dashboard | `dashboard_2.html` … `dashboard_5.html` |
| Danh sách có bộ lọc | `contacts.html`, `clients.html` |
| Hộp thoại, thông báo | `modal_window.html`, `notifications.html` |
| Trạng thái rỗng / lỗi | `404.html`, `500.html` |
| Biểu đồ | `c3.html`, `chart_flot.html`, `morris.html` |

Cột bên phải là để **nhìn**, không phải để chép.

---

## Việc cần làm khi `apps/admin-web` bắt đầu

1. Đổ bảng màu, thang chữ và thang khoảng cách ở trên thành **CSS custom properties** trong một file
   token duy nhất, ánh xạ sang biến `--bs-*` của Bootstrap 5. Một nơi đổi, cả app đổi theo.
2. Dựng `.ibox` thành component React đầu tiên. Mọi màn hình sau đó là nó lặp lại.
3. Nạp CSS của Bootstrap 5.3.8 rồi ghi đè bằng token — không dùng theme dựng sẵn, không cài
   `bootstrap.js`, không cài jQuery.
4. Tự host Open Sans + Font Awesome từ file trong bộ mẫu.
5. Đo lại mật độ bảng ở 16px trước khi dựng màn hình danh sách đầu tiên.
6. Một màn lỗi hiện `corrId` cho người dùng đọc cho CSKH (`AD-23`).
7. Chuỗi giao diện tiếng Việt (`AGENTS.md`).

---

## Responsive — ưu tiên đã chốt: cả mobile lẫn PC

Điểm ngắt dùng mặc định của Bootstrap 5: `576` · `768` · `992` · `1200` · `1400`.

| Vùng | Dưới `md` (< 768) | Từ `lg` (≥ 992) |
|---|---|---|
| Sidebar | ẩn, mở dạng off-canvas | cố định, thu gọn được thành dải icon |
| Card `.ibox` | một cột | lưới theo nội dung |
| Bảng dữ liệu | tuỳ loại màn hình — xem dưới | lưới đầy đủ |
| Biểu đồ | chiều cao cố định, ẩn bớt nhãn trục | đầy đủ |

**Bảng dữ liệu — chỉ dashboard và CRUD mới làm màn hình hẹp:**

| Loại màn hình | Màn hình hẹp |
|---|---|
| **Báo cáo** | **Không áp dụng.** Giữ lưới đầy đủ, cuộn ngang. Báo cáo là việc của máy để bàn |
| **Dashboard** | Có — thẻ xếp một cột, biểu đồ co theo vùng chứa |
| **CRUD** | Có — lưới chuyển **danh sách thẻ** dưới `md`, mỗi dòng thành một thẻ 3–4 trường + nút thao tác |

Nhờ vậy bảy màn hình báo cáo — phần tốn công nhất, ít người mở trên điện thoại nhất — không phải làm
responsive. Chi tiết ở `ADR-006`.

**Vùng chạm.** Chuẩn trợ năng đòi vùng chạm tối thiểu ~44px, trong khi màn hình quản trị lại cần dày đặc.
Hoà giải bằng con trỏ chứ không bằng bề rộng màn hình:

```
@media (pointer: coarse) { /* nới padding hàng, nút, ô chọn */ }
```

Máy điều phối viên dùng chuột thì giữ mật độ cao; điện thoại và máy tính bảng thì nới ra. Đây là hai
trục khác nhau — màn hình rộng vẫn có thể là màn hình cảm ứng.

**ECharts không tự co giãn.** Gắn `ResizeObserver` lên vùng chứa, không chỉ nghe `window.resize`:
sidebar thu gọn làm đổi kích thước vùng chứa mà cửa sổ không đổi. Chi tiết ở `ADR-006`.

**Chưa biết, cần trả lời:** ai dùng bản mobile và để làm gì. Điều phối viên ngồi bàn thì gần như chắc
chắn dùng PC; quản lý xem báo cáo trên điện thoại là một kịch bản khác hẳn về màn hình cần có. Làm
responsive cho *mọi* màn hình là tốn công cho những màn hình không ai mở trên điện thoại. Cần danh sách
màn hình ưu tiên cho mobile.

---

## Đã chốt

- **Bản quyền INSPINIA:** đã có, xác nhận của chủ sản phẩm ngày 2026-09-18. Được dùng bộ mẫu làm tham
  chiếu bố cục và hình dạng.
- **Thư viện component:** `react-bootstrap` 2.10.10 trên `bootstrap` 5.3.8 — xem
  `docs/adr/ADR-005-thu-vien-component-frontend.md`.

  Một ràng buộc từ ADR đó phải nhớ khi viết màn hình: **không truyền class component làm con của**
  `Modal`, `Fade`, `Collapse`, `Overlay`, `OverlayTrigger`, `Tooltip`, `Popover`, `Dropdown`. React 19 đã
  gỡ `ReactDOM.findDOMNode`, và đó là đường duy nhất trong `react-bootstrap` còn chạm tới nó — hỏng **lúc
  chạy**, không phải lúc build.
- **Bảng dữ liệu:** AG Grid Enterprise 36.2.0, **làm tương tự web 1** — không nạp khoá bản quyền, chấp
  nhận watermark hiện trên lưới ở mọi môi trường. **Biểu đồ:** Apache ECharts 6.1.0. Xem
  `docs/adr/ADR-006-bang-du-lieu-va-bieu-do.md`.
- **Chế độ tối: bỏ.** Không làm. Công sức dồn cho responsive trên cả mobile lẫn PC.

## Chưa trả lời

- **Màn hình nào ưu tiên cho mobile.** Đã biết báo cáo thì không; còn lại dashboard và CRUD nào thực sự
  có người mở trên điện thoại thì chưa rõ.
- **Ngưỡng và trường hiển thị của danh sách thẻ** cho từng màn hình CRUD.
