# Nền Tảng Plugin Runtime

Hệ thống chạy plugin bảo mật, điều khiển bằng metadata — cho phép tải và thực thi plugin động tại runtime, được quản lý bởi manifest có chữ ký số và kiểm soát truy cập dựa trên capability.

## Hệ Thống Làm Gì

Nền tảng này cho phép tổ chức chạy mã plugin không tin cậy trong môi trường cách ly an toàn. Mọi plugin đều phải vượt qua xác minh chữ ký số trước khi thực thi, và chỉ có thể truy cập tài nguyên đã khai báo rõ ràng trong manifest.

## Các Thành Phần

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Nền Tảng Plugin Runtime                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌──────────────────┐   ┌──────────────────┐   ┌────────────────┐  │
│  │ Cổng Marketplace │   │ Cổng Consumer    │   │ Cổng Admin     │  │
│  │                  │   │                  │   │                │  │
│  └────────┬─────────┘   └────────┬─────────┘   └───────┬────────┘  │
│           │                      │                      │           │
│           └──────────────────────┼──────────────────────┘           │
│                                  │                                   │
│                    ┌─────────────▼─────────────┐                    │
│                    │   API Gateway Công Khai    │                    │
│                    └─────────────┬─────────────┘                    │
│                                  │                                   │
│                    ┌─────────────▼─────────────┐                    │
│                    │   API Backend Hợp Nhất    │                    │
│                    └───────────────────────────┘                    │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Các Dự Án

| # | Dự án | Mô tả |
|---|-------|-------|
| 1 | **Unified API** | Backend cốt lõi. Quản lý tenant, thanh toán, gói đăng ký, plugin package, và đồng bộ gateway. Toàn bộ logic nghiệp vụ nằm ở đây. |
| 2 | **Public API Gateway** | Cổng vào công khai cho người dùng API. Xác thực bằng API key, kiểm soát rate limit và quota hàng ngày, đo lường sử dụng, và chuyển tiếp request đến backend. |
| 3 | **Plugin Runtime Engine** | Động cơ thực thi. Tải plugin DLL vào sandbox cách ly, xác minh chữ ký, phân giải capability, và thực thi plugin an toàn. |
| 4 | **Marketplace Portal** | Giao diện web cho nhà phát triển plugin. Duyệt, tìm kiếm, tải lên, và quản lý extension. Xem quyền và đăng ký extension khác. |
| 5 | **Consumer Portal** | Giao diện web cho người dùng API. Xem phân tích sử dụng, quản lý API key, xử lý thanh toán, đổi gói, và truy cập tài liệu. |
| 6 | **Admin Portal** | Giao diện quản trị nội bộ cho vận hành viên. Duyệt plugin, quản lý tenant, xem kết quả quét bảo mật, và giám sát hệ thống. |

## Tính Năng Chính

**Cho Nhà Phát Triển Plugin**
- Tải lên và xuất bản plugin qua Marketplace
- Khai báo capability và quyền trong manifest
- Đăng ký extension khác để giao tiếp giữa các plugin
- Theo dõi lượt sử dụng và số người đăng ký

**Cho Người Dùng API**
- Tự đăng ký với lựa chọn gói (Free / Pro / Enterprise)
- Quản lý API key với xoay key và hạn sử dụng
- Phân tích sử dụng realtime với biểu đồ
- Thanh toán tự động qua Stripe

**Cho Vận Hành Viên**
- Mô hình bảo mật zero-trust — mọi plugin đều không tin cậy cho đến khi xác minh
- Xác minh chữ ký số trước khi thực thi
- Kiểm soát truy cập dựa trên capability — plugin chỉ dùng được tài nguyên đã khai báo
- Cách ly đa tenant với rate limit và quota riêng từng tenant
- Lịch sử kiểm toán đầy đủ cho mọi hành động quản trị

**Hạ Tầng**
- Hỗ trợ nhiều loại database: PostgreSQL, SQLite, hoặc JSON file
- Redis cho cache, rate limiting, và thông báo realtime
- OpenTelemetry cho distributed tracing và metrics
- .NET Aspire orchestration — khởi động toàn bộ bằng một lệnh

## Bắt Đầu

```bash
# Chạy toàn bộ nền tảng với Aspire (cần Docker)
cd src/Aspire/PluginRuntime.AppHost
dotnet run

# Hoặc chạy từng project riêng
cd src/PluginRuntime.Api
dotnet run

cd src/PublicApiGateway
dotnet run
```

## Cấu Trúc Dự Án

```
src/
├── Aspire/                    → Điều phối (chạy tất cả cùng lúc)
├── PluginRuntime.Api/         → API backend hợp nhất (modular monolith)
├── PublicApiGateway/          → API gateway công khai
├── Core/                      → Động cơ runtime plugin
├── Marketplace/               → Marketplace cho nhà phát triển (web frontend)
├── ConsumerPortal/            → Cổng người dùng API (web frontend)
├── Admin/                     → Cổng quản trị
├── SDK/                       → SDK phát triển plugin
├── Capabilities/              → Tầng truy cập hạ tầng cho plugin
├── Infrastructure/            → Tích hợp database và dịch vụ bên ngoài
└── Tests/                     → Tất cả test project
```

## Giấy Phép

Sở hữu riêng. Bảo lưu mọi quyền.
