# Staxi.TenantLeakTests — bộ test rò rỉ hai tenant (AD-24)

Bộ test đầu tiên của dự án, viết **trước** tính năng đầu tiên. Nó tồn tại vì mọi AD cách ly khác
đều đúng trên giấy cho tới khi có thứ chứng minh.

Hai hãng giả: `hang-a` và `hang-b`, **mỗi hãng một database riêng** (SQLite, đúng hình dạng của 18 DB thật).
Mọi bản ghi của hãng B đều mang chữ `HANG-B` trong tên — thấy chuỗi đó trong phản hồi của hãng A là rò rỉ,
không phải chuyện phải suy luận.

## Đang phủ gì

| Tệp | Vế của AD-24 | AD liên quan |
|---|---|---|
| `Tests/PhamViHttpTests.cs` | Gọi HTTP bằng token hãng A xin dữ liệu hãng B — qua query, header, body, token ký khoá lạ | AD-2, AD-3, AD-4, AD-8, AD-14 |
| `Tests/PhamViSignalRTests.cs` | Phát tin ở hãng A, khẳng định hãng B **không nhận gì** | AD-5, AD-7 |
| `Tests/PhamViCacheTests.cs` | Nạp cache ở hãng A, đọc ở hãng B | AD-6, AD-17 |
| `Tests/PhamViDatabaseTests.cs` | Truy vấn qua repository không phạm vi; kết nối lệch database | AD-2, AD-4, AD-5, AD-18 |
| `Tests/PhamViRoRiSangViecNenTests.cs` | Phạm vi rò sang công việc chạy nền; bất biến của `TenantScope` | AD-4, AD-15 |

## Bộ test này có răng — đã kiểm bằng cách gỡ từng lớp bảo vệ

| Gỡ cái gì | Bao nhiêu bài đỏ |
|---|---|
| `CacheKey` bỏ tiền tố tenant | 4 |
| `ITenantConnectionFactory` luôn mở database hãng B | 7 |
| `ITenantNotifier` phát bằng `Clients.All` | 2 |

Làm lại phép kiểm này mỗi khi thêm một cơ chế cách ly: một bài test không đỏ khi lớp bảo vệ bị gỡ
là một bài test không kiểm gì cả.

## Chạy

```bash
dotnet test tests/Staxi.TenantLeakTests/Staxi.TenantLeakTests.csproj
```

Chạy **tuần tự**, có chủ ý — xem `AssemblyInfo.cs`.

## Khi thêm một cơ chế cách ly mới

AD-24 buộc mở rộng bộ này mỗi lần. Thứ tự làm: viết bài test đỏ trước, rồi mới viết cơ chế, rồi gỡ
cơ chế ra một lần nữa để chắc bài test thật sự đỏ.

## Còn thiếu — biết trước khi bị bất ngờ

- **Chỉ chạy trên SQLite.** Nó kiểm đường đi của phạm vi, không kiểm phương ngữ SQL Server, không kiểm
  `Encrypt=true` (xem bảng Stack trong spine), không kiểm 18 login SQL riêng của AD-19.
- **Chưa có Redis thật.** Cache đang là `MemoryTenantCache`; backplane SignalR chạy trong một tiến trình.
  Rò rỉ xuyên tiến trình qua backplane Redis chưa được bài nào chạm tới.
- **Chưa có bài kiểm cấu trúc.** Lệnh cấm của AD-4 theo *khái niệm*, còn
  `ScopeParameterGuardMiddleware` chỉ chặn theo *tên*. Phần quét DTO và chữ ký tham số thuộc
  `Staxi.ArchitectureTests` (AD-15) — chưa dựng.
- **Chưa nối CI.** AD-24 đòi chạy mọi lần; hiện chưa có workflow nào gọi nó.
