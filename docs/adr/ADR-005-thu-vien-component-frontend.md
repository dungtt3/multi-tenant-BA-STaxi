# ADR-005 — Thư viện component cho frontend

- **Trạng thái:** Chốt
- **Ngày:** 2026-09-18
- **Liên quan:** `AD-1` (FE build tĩnh, không framework cần Node phía server), `docs/design-guide.md` (primary `#337ab7`, gốc chữ 16px, Bootstrap 5.3.8), `AD-23` (tư thế bảo mật trình duyệt)

---

## Bối cảnh

`apps/admin-web` chưa có một dòng nào. Trước màn hình đầu tiên phải trả lời: dựng component bằng gì.

Câu hỏi này không hoãn được. Lớp component là thứ mọi màn hình sau đó kế thừa — đổi nó ở màn hình thứ
mười là viết lại chín màn hình đầu.

Ba ràng buộc đã chốt từ trước:

- `AD-1`: React 19.3.0 + Vite 8.3.0 + TypeScript 5.9.3, build ra **file tĩnh**.
- `docs/design-guide.md`: **Bootstrap 5.3.8**, bảng màu riêng (primary `#337ab7`), gốc chữ 16px.
- Bộ mẫu thị giác là INSPINIA — vốn là một theme Bootstrap. Bản quyền đã có (xác nhận của chủ sản phẩm,
  2026-09-18), nên được dùng làm tham chiếu bố cục và hình dạng.

### Đã đo được — kiểm chứng trên npm ngày 2026-09-18

| Gói | Bản | Peer |
|---|---|---|
| `react-bootstrap` | 2.10.10 | `react >= 16.14.0`, `react-dom >= 16.14.0`, `@types/react >= 16.14.8` |
| `bootstrap` | 5.3.8 | — |
| `react-transition-group` | 4.4.5 | phụ thuộc bắc cầu của `react-bootstrap` |

`react-bootstrap` **không** đóng gói CSS của Bootstrap — nó là bản dựng lại các component bằng React,
không dùng `bootstrap.js`, không dùng jQuery. CSS do mình mang tới. Điều đó hợp với cả `AD-1` lẫn kế
hoạch ghi đè token bằng biến `--bs-*` của Bootstrap 5.

---

## Cái bẫy đã tìm ra, và vì sao nó không chặn quyết định

React 19 **đã gỡ bỏ `ReactDOM.findDOMNode`**. Gọi tới nó là `TypeError` **lúc chạy**, không phải lỗi biên
dịch — cùng họ với cái bẫy `StackExchange.Redis` ghi trong bảng Stack của spine: *build vẫn xanh, lỗi chỉ
hiện lúc chạy*.

Trong `react-bootstrap@2.10.10` có `safeFindDOMNode.js`:

```js
export default function safeFindDOMNode(componentOrElement) {
  if (componentOrElement && 'setState' in componentOrElement) {
    return ReactDOM.findDOMNode(componentOrElement);
  }
  return componentOrElement != null ? componentOrElement : null;
}
```

Dùng ở `Overlay.js`, `OverlayTrigger.js`, `TransitionWrapper.js` — tức là đường đi của Modal, Fade,
Collapse, Tooltip, Popover, Dropdown. Đó là phần lớn UI của một trang quản trị.

Hai điều đã kiểm tận nơi trong mã nguồn gói:

1. **`react-transition-group@4.4.5` có bốn chỗ gọi `ReactDOM.findDOMNode(this)`**, nhưng cả bốn đều dạng
   `this.props.nodeRef ? ... : ReactDOM.findDOMNode(this)`. Mà `TransitionWrapper` của `react-bootstrap`
   **có truyền `nodeRef`** (`nodeRef: nodeRef` trong lời gọi `Transition`). Nên nhánh `findDOMNode`
   **không bao giờ chạy** qua đường này.

2. Chỗ còn lại là `attachRef → safeFindDOMNode(r)`. Nó chỉ gọi `findDOMNode` khi `r` là **thể hiện của
   class component** (`'setState' in r`). Truyền vào một DOM node hay một function component có
   `forwardRef` thì nó trả về nguyên xi.

**Kết luận:** `react-bootstrap` 2.10.10 chạy được trên React 19, với đúng một điều kiện — xem *Hệ quả*.

---

## Yếu tố quyết định

| | Vì sao nó nặng ở dự án này |
|---|---|
| Trợ năng có sẵn | Modal bẫy tiêu điểm, Dropdown điều hướng bằng bàn phím, `aria-*` đúng. Tự viết lại là nơi sinh lỗi âm thầm nhất, và đội không có chuyên gia trợ năng. |
| Khớp bộ mẫu | INSPINIA là theme Bootstrap. Chọn thư viện khác hệ là vứt bỏ 216 trang mẫu vừa được cấp bản quyền. |
| Đội nhỏ, nền tảng nhiều năm | Thời gian nên đổ vào cách ly tenant (`AD-2`…`AD-7`), không phải vào việc dựng lại dropdown. |
| Token phải ghi đè được | Bootstrap 5.3 phơi `--bs-*` ra CSS custom properties; `react-bootstrap` không mang CSS riêng nên không đánh nhau với token. |
| Không thêm runtime | `AD-1` cấm thứ cần Node phía server. `react-bootstrap` là thư viện build-time thuần. |

---

## Các phương án

### A. `react-bootstrap` trên Bootstrap 5.3.8 ⭐ *(chọn)*

Component React thuần, CSS Bootstrap do mình mang tới và ghi đè bằng token.

- ✅ Trợ năng của Modal/Dropdown/Overlay đã có người lo
- ✅ Khớp thẳng bộ mẫu INSPINIA và design guide
- ✅ Không jQuery, không `bootstrap.js` — hợp `AD-1`
- ⚠️ Bám theo Bootstrap 5; khi Bootstrap 6 ra sẽ có độ trễ
- ⚠️ Có `safeFindDOMNode` — ràng buộc ở *Hệ quả*

### B. Dựng thuần từ token, không thư viện

- ✅ Kiểm soát hoàn toàn, không phụ thuộc thêm
- ❌ Phải tự làm trợ năng cho từng component. Với một hệ 565 bảng dữ liệu, đây là công việc không có điểm dừng
- ❌ Chi phí đổ vào chỗ không phải bài toán của dự án

### C. Headless (Radix, Headless UI) + CSS Bootstrap

- ✅ Trợ năng tốt, không áp đặt kiểu dáng
- ⚠️ Vẫn phải tự ghép kiểu cho từng component để ra hình dạng INSPINIA
- ❌ Hai hệ khái niệm chồng nhau: class của Bootstrap và cấu trúc của headless. Tốn công hoà giải hơn là được lợi

### D. Thư viện khác hệ (MUI, Mantine, Ant Design)

- ✅ Component phong phú, bảng dữ liệu mạnh
- ❌ Đổi hẳn ngôn ngữ thị giác, vứt bỏ bộ mẫu và tính liền mạch với web cũ
- ❌ Gói lớn hơn hẳn, và ép theo hệ thiết kế riêng của nó

---

## Quyết định

**Phương án A.** `react-bootstrap` 2.10.10 trên `bootstrap` 5.3.8.

---

## Hệ quả

- **Cấm truyền class component làm con của các component có hiệu ứng chuyển** — `Modal`, `Fade`,
  `Collapse`, `Overlay`, `OverlayTrigger`, `Tooltip`, `Popover`, `Dropdown`. Đó là đường duy nhất chạm tới
  `ReactDOM.findDOMNode`, và trên React 19 nó ném lỗi **lúc chạy**.
  Thực tế đây là ràng buộc rẻ: code mới viết toàn function component. Nhưng nó phải thành một dòng lint
  hoặc một mục trong review, không phải một điều "ai cũng biết".
- **Ghim ba phiên bản**, không tự nâng: `react-bootstrap` 2.10.10, `bootstrap` 5.3.8, và
  `react-transition-group` phải ở nhánh 4.x có `nodeRef`.
- CSS của Bootstrap do mình nạp và ghi đè bằng token trong `docs/design-guide.md`. Không dùng theme dựng sẵn.
- Không cài `bootstrap.js`, không cài jQuery. Có ai thêm vào là dấu hiệu đang chép markup từ bộ mẫu — việc
  mà design guide cấm.
- Ba dòng phiên bản này **chưa có trong bảng Stack của spine**. Theo `AGENTS.md`, phải ghi vào
  `docs/.architecture-memlog.md` trước, rồi mới cập nhật spine — một việc riêng, có review.

---

## Việc còn mở

- [ ] Dựng một bài kiểm tự động chặn `findDOMNode` chạy vào đường Overlay/Modal — hoặc một quy tắc lint cấm
      class component trong `apps/admin-web`
- [ ] Bảng dữ liệu: `react-bootstrap` chỉ có `<Table>` trần. Màn hình danh sách cần sắp xếp, phân trang,
      cột co giãn. Chọn thư viện bảng (TanStack Table?) là một quyết định riêng, chưa chốt
- [ ] Biểu đồ: bộ mẫu dùng c3/flot/morris — đều là jQuery. Cần chọn thư viện biểu đồ cho React
- [ ] Khi Bootstrap 6 ra, `react-bootstrap` 2.x sẽ trễ bao lâu, và ta chấp nhận trễ tới mức nào
