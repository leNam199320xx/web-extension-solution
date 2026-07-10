# Hướng Dẫn Phát Triển Extension

Hướng dẫn này giải thích cách thiết kế, xây dựng và xuất bản extension trên nền tảng Plugin Runtime.

## Extension Là Gì?

Extension là một đơn vị chức năng độc lập chạy trong sandbox cách ly của nền tảng. Nó nhận đầu vào, thực hiện công việc thông qua các capability đã khai báo, và trả về đầu ra — không có quyền truy cập trực tiếp vào hệ thống chủ.

Extension có thể:
- Đọc/ghi dữ liệu qua capability Database
- Gọi HTTP qua capability Network
- Lưu/đọc file qua capability Storage
- Dùng cache qua capability Cache
- Gọi extension khác qua capability Extension

## Vòng Đời Extension

```
Bạn xây dựng → Tải lên → Nền tảng quét bảo mật → Admin duyệt → 
Nền tảng ký số → Người dùng có thể gọi → Runtime xác minh + thực thi
```

1. **Xây dựng** — Viết code, tạo manifest, đóng gói thành `.plugin.zip`
2. **Tải lên** — Qua Marketplace Portal hoặc CLI
3. **Quét bảo mật** — Nền tảng tự động quét tìm lỗ hổng
4. **Phê duyệt** — Admin xem xét và phê duyệt
5. **Ký số** — Nền tảng ký manifest bằng private key
6. **Xuất bản** — Extension sẵn sàng để gọi
7. **Thực thi** — Runtime xác minh chữ ký → phân giải capability → chạy trong sandbox

## Cấu Trúc Project

```
my-extension/
├── manifest.json          → Khai báo danh tính, phiên bản, capability, quyền
├── MyExtension.dll        → Code extension đã biên dịch
└── (các dependency tùy chọn)
```

## Manifest

Manifest là file quan trọng nhất. Nó cho nền tảng biết extension của bạn là gì và cần gì.

```json
{
  "extension_id": "com.acme.data-processor",
  "version": "1.2.0",
  "name": "Acme Data Processor",
  "description": "Xử lý và chuyển đổi bản ghi dữ liệu",
  "author": "Acme Corp",
  "entry_point": "AcmeDataProcessor.dll",
  "entry_class": "Acme.DataProcessor.Plugin",
  
  "capabilities": [
    "database",
    "network",
    "cache"
  ],
  
  "permissions": [
    {
      "capability": "database",
      "scope": "read_write",
      "justification": "Lưu kết quả xử lý vào database của tenant"
    },
    {
      "capability": "network",
      "scope": "outbound_https",
      "justification": "Lấy dữ liệu từ API bên ngoài để làm giàu data"
    },
    {
      "capability": "cache",
      "scope": "read_write",
      "justification": "Cache kết quả từ API bên ngoài trong 5 phút"
    }
  ],

  "visibility": "public",
  "resource_limits": {
    "max_memory_mb": 256,
    "max_execution_seconds": 30,
    "max_cpu_percent": 50
  }
}
```

## Viết Code Extension

Extension implement interface `IPlugin` từ SDK:

```csharp
using PluginRuntime.Sdk;

public class Plugin : IPlugin
{
    public async Task<PluginResult> ExecuteAsync(
        PluginContext context, 
        CancellationToken ct)
    {
        // Truy cập capability đã khai báo qua context
        var db = context.GetCapability<IDatabaseCapability>();
        var cache = context.GetCapability<ICacheCapability>();
        
        // Đọc đầu vào
        var input = context.Input;
        
        // Xử lý...
        var cachedResult = await cache.GetAsync<string>("my-key", ct);
        if (cachedResult is null)
        {
            var data = await db.QueryAsync("SELECT ...", ct);
            await cache.SetAsync("my-key", data, TimeSpan.FromMinutes(5), ct);
            cachedResult = data;
        }
        
        // Trả về đầu ra
        return PluginResult.Success(new { processed = true, data = cachedResult });
    }
}
```

## Các Capability Có Sẵn

| Capability | Cung cấp | Ví dụ |
|-----------|----------|-------|
| **Database** | Đọc/ghi database trong phạm vi tenant | Lưu kết quả, truy vấn bản ghi |
| **Network** | Gọi HTTP/HTTPS ra ngoài | Gọi API bên ngoài, lấy dữ liệu |
| **Storage** | Đọc/ghi file vào object storage | Lưu báo cáo, import/export file |
| **Cache** | Đọc/ghi giá trị tạm | Cache kết quả tính toán nặng |
| **Extension** | Gọi extension khác | Chuỗi workflow, chia sẻ data giữa plugin |

## Luật Capability

- Chỉ dùng được capability đã khai báo trong manifest
- Cố gắng dùng capability không khai báo sẽ bị chặn ngay tại runtime
- Mỗi capability có scope (read_only, read_write, outbound_https, v.v.)
- Phải cung cấp lý do cho mỗi quyền — admin sẽ xem xét

## Giao Tiếp Giữa Extension

Extension có thể gọi extension khác qua capability Extension:

```csharp
var ext = context.GetCapability<IExtensionCapability>();

var result = await ext.InvokeAsync(
    extensionId: "com.partner.enrichment-service",
    input: new { recordId = "abc123" },
    ct);
```

Luật khả năng hiển thị kiểm soát ai có thể gọi ai:
- **Public** — Bất kỳ extension nào đều gọi được
- **Private** — Chỉ extension cùng publisher
- **Subscription** — Cần gửi yêu cầu và được chủ sở hữu phê duyệt

## Đóng Gói

Đóng gói extension thành file zip với đuôi `.plugin.zip`:

```
my-extension.plugin.zip
├── manifest.json
├── MyExtension.dll
└── (các DLL dependency)
```

## Tải Lên & Xuất Bản

**Qua Marketplace Portal:**
1. Vào Marketplace Portal → Upload
2. Kéo thả file `.plugin.zip`
3. Xem lại manifest và quyền đã phân tích
4. Gửi để được duyệt

**Qua API:**
```bash
curl -X POST https://api.example.com/api/plugins/upload \
  -H "Authorization: Bearer <token>" \
  -F "file=@my-extension.plugin.zip"
```

## Quản Lý Phiên Bản

- Mỗi lần tải lên tạo phiên bản mới
- Số phiên bản theo semver (1.0.0, 1.1.0, 2.0.0)
- Nhiều phiên bản có thể cùng tồn tại — consumer chỉ định phiên bản khi gọi
- Phiên bản cũ có thể deprecated nhưng vẫn gọi được cho đến khi bị thu hồi

## Giới Hạn Tài Nguyên

Mỗi extension chạy trong giới hạn:

| Giới hạn | Mặc định | Mục đích |
|----------|---------|---------|
| Bộ nhớ | 256 MB | Ngăn sử dụng bộ nhớ quá mức |
| Thời gian thực thi | 30 giây | Ngăn extension treo |
| CPU | 50% một core | Ngăn thiếu CPU cho tiến trình khác |

Vượt giới hạn sẽ hủy thực thi ngay lập tức.

## Mô Hình Bảo Mật

Nền tảng theo nguyên tắc zero-trust:

1. **Không tin tưởng mặc định** — Extension không thể làm gì cho đến khi được cấp phép rõ ràng
2. **Manifest là luật** — Những gì khai báo là tối đa bạn được truy cập
3. **Manifest có chữ ký** — Manifest bị sửa đổi sẽ bị từ chối tại runtime
4. **Xác minh hash** — Binary bị thay đổi sẽ bị từ chối tại runtime
5. **Thực thi cách ly** — Mỗi lần thực thi chạy trong sandbox riêng

## Mẹo Cho Nhà Phát Triển

- Chỉ yêu cầu capability thực sự cần
- Viết lý do rõ ràng — giúp duyệt nhanh hơn
- Xử lý timeout — luôn tôn trọng CancellationToken
- Trả về output có cấu trúc — dễ cho extension khác tiêu thụ
- Đánh phiên bản đúng — thay đổi phá vỡ phải tăng major version
- Giữ ít dependency — ít DLL hơn = tải nhanh hơn, bề mặt tấn công nhỏ hơn
