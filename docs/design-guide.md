# Design guide — WEB2

Nguồn: `C:\Users\dungtt3\Files\HTML5_Full` — **INSPINIA Responsive Admin Theme**, Bootstrap 4.1.0.
216 trang HTML mẫu, 81 CSS, 853 ảnh.

## Vì sao là bộ này, và vì sao điều đó quan trọng hơn vẻ ngoài

`BA.STaxi.Web` **đang chạy chính theme này**. Đếm trong `BA.STaxi.Web/Content`: `#1ab394` xuất hiện
**314 lần**, `#e7eaec` **351 lần**, `#2f4050` **39 lần**.

WEB2 mọc bên cạnh web cũ trên một domain mới và từng màn hình chuyển dần (Strangler Fig, xem
`docs/ARCHITECTURE-SPINE.md`). Một điều phối viên tổng đài sẽ dùng **cả hai** trong cùng một ca làm
việc, có khi cùng một giờ. Nếu WEB2 trông như một sản phẩm khác thì mỗi màn hình chuyển sang là một
lần đào tạo lại, và `AD-20` (sổ chủ sở hữu màn hình) biến thành sổ ghi những lần người dùng phàn nàn.

Giữ nguyên ngôn ngữ thị giác là cách rẻ nhất để việc chuyển đổi **không ai để ý**.

---

## Luật nền: lấy thiết kế, không lấy code

| | |
|---|---|
| ✅ Lấy | bảng màu · thang chữ · thang khoảng cách · hình dạng component · bố cục màn hình · ảnh chụp 216 trang mẫu làm tham chiếu |
| ⛔ Không lấy | file `.js` · markup jQuery · `metisMenu` · `bootstrap.js` · `.less`/`.scss` của theme |

`AD-1` quy định FE là **Vite + React 19 + TypeScript, build ra file tĩnh**. Chép markup jQuery vào
React là đi ngược kiến trúc và tạo ra thứ không ai bảo trì nổi: một cây DOM do jQuery điều khiển nằm
trong một cây do React điều khiển.

Cách dùng đúng: mở trang mẫu trong trình duyệt, **nhìn**, rồi dựng lại bằng component React của mình
với token bên dưới.

---

## Token

### Màu

| Vai trò | Mã | Dùng ở đâu |
|---|---|---|
| Chủ đạo | `#1ab394` | nút chính, link, trạng thái hoạt động, viền trên của card khi nhấn mạnh |
| Nền sidebar | `#2f4050` | thanh điều hướng trái, và là `background-color` của `body` |
| Sidebar hover | `#293846` | mục menu khi rê chuột / focus |
| Chữ trong sidebar | `#a7b1c2` | mục menu thường; mục đang chọn chuyển `#ffffff` |
| Nền vùng nội dung | `#f3f3f4` | nền phía sau các card |
| Nền card | `#ffffff` | `.ibox-title`, `.ibox-content` |
| Viền / đường kẻ | `#e7eaec` | mọi đường kẻ ngang, viền card, viền bảng |
| Chữ thường | `#676a6c` | body text |
| Chữ mờ | `#999999` | chú thích, nhãn phụ |

Màu trạng thái:

| Trạng thái | Mã |
|---|---|
| Thành công / chủ đạo | `#1ab394` |
| Thông tin | `#23c6c8` |
| Xanh dương | `#1c84c6` |
| Cảnh báo | `#f8ac59` |
| Nguy hiểm | `#ed5565` |

> **Cảnh báo về màu trạng thái:** `#1ab394` vừa là màu thương hiệu vừa là màu "thành công". Khi một
> nút chính nằm cạnh một badge trạng thái, người dùng không phân biệt được đâu là hành động đâu là
> tình trạng. Ở màn hình có nhiều trạng thái — điều xe, chuyến đi — dùng badge có chữ, đừng dùng
> chấm màu trần.

### Chữ

```
font-family: "Open Sans", "Helvetica Neue", Helvetica, Arial, sans-serif;
font-size: 13px;
color: #676a6c;
```

13px là nhỏ so với chuẩn hôm nay. **Giữ nguyên** — web cũ đang 13px, và hai hệ đặt cạnh nhau mà lệch
cỡ chữ thì trông như lỗi hiển thị. Nếu muốn nâng, đó là một quyết định cho **cả hai** hệ, không phải
việc WEB2 tự làm.

Font `Open Sans` và bộ icon (Font Awesome, Glyphicons) đã có sẵn file `.woff`/`.ttf` trong bộ. Tự
host, đừng gọi CDN — 18 hãng chạy trên mạng nội bộ.

### Card — `.ibox`

Đơn vị bố cục cơ bản của INSPINIA. Mọi khối nội dung đều nằm trong một `.ibox`.

```
.ibox          margin-bottom: 25px
.ibox-title    nền #ffffff · viền trên 2px #e7eaec · không viền dưới
.ibox-content  nền #ffffff · padding 15px 20px 20px 20px · viền trên-dưới 1px #e7eaec
```

Đặc điểm nhận dạng: **viền trên 2px** trên tiêu đề card, không bo góc, không đổ bóng. Đây là thứ làm
INSPINIA trông ra INSPINIA — bỏ nó đi là màn hình lạc khỏi web cũ ngay cả khi màu vẫn đúng.

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
│  menu nhiều  │  │ .ibox                          │  │
│  cấp         │  └────────────────────────────────┘  │
│              │  nền #f3f3f4                         │
└──────────────┴──────────────────────────────────────┘
```

Sidebar thu gọn được thành dải icon (`body.mini-navbar`). Giữ hành vi này — người dùng web cũ đã quen.

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

Cột "mở trang nào" là để **nhìn**, không phải để chép.

---

## Việc cần làm khi `apps/admin-web` bắt đầu

1. Đổ bảng màu và thang khoảng cách ở trên thành **CSS custom properties** trong một file token duy
   nhất. Một nơi đổi, cả app đổi theo.
2. Quyết định thư viện component **trước màn hình đầu tiên**. INSPINIA là Bootstrap 4; React có
   react-bootstrap, hoặc dựng thuần. Đây là quyết định kiến trúc, cần một ADR — chưa ai chốt.
3. Tự host Open Sans + Font Awesome từ file trong bộ.
4. Dựng `.ibox` thành component React đầu tiên. Mọi màn hình sau đó đều là nó lặp lại.
5. Chuỗi giao diện tiếng Việt, theo lệ sẵn có (`AGENTS.md`).

---

## Chưa trả lời

- **Bản quyền INSPINIA.** Đây là theme thương mại. Web cũ đã dùng nên giấy phép nhiều khả năng có sẵn,
  nhưng **chưa ai kiểm tra** nó phủ thêm một sản phẩm mới hay chỉ phủ đúng một site. Việc này phải
  làm rõ trước khi hãng đầu tiên lên production, không phải sau.
- **Bootstrap 4 đã hết hỗ trợ.** Nếu dùng react-bootstrap thì nó nhắm Bootstrap 5, và Bootstrap 5 đổi
  tên nhiều class cùng hệ lưới. Hoặc chấp nhận lệch, hoặc dựng thuần từ token. Một ADR nữa.
- **Chế độ tối.** Bộ này không có. Đừng tự thêm khi web cũ không có — hai hệ cạnh nhau sẽ lệch.
