# Hướng Dẫn Đăng Nhập Demo

Cách đăng nhập và sử dụng nền tảng demo. Không cần dịch vụ bên ngoài.

## Khởi Động Nền Tảng

```bash
# Double-click run-all.bat, hoặc:
cd h:\namld1\web-extension-solution
run-all.bat
```

Các service chạy tại:
- **API Backend** — http://localhost:6100
- **Swagger UI** — http://localhost:6100/swagger
- **API Gateway** — http://localhost:6200
- **Marketplace Portal** — http://localhost:6300
- **Consumer Portal** — http://localhost:6400
- **Admin Portal** — http://localhost:6500

## Đăng Nhập Qua API

### Đăng Ký Tài Khoản Mới

```bash
curl -X POST http://localhost:6100/api/auth/register ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"ban@example.com\",\"password\":\"matkhau123\",\"displayName\":\"Tên Bạn\"}"
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "displayName": "Tên Bạn",
  "email": "ban@example.com",
  "role": "Tenant_Owner",
  "tenantId": "...",
  "expiresAt": "2024-07-10T10:00:00Z"
}
```

### Đăng Nhập Tài Khoản Có Sẵn

```bash
curl -X POST http://localhost:6100/api/auth/login ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"alice@acme.com\",\"password\":\"secret\"}"
```

### Dùng Token

```bash
curl http://localhost:6100/api/auth/me ^
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

## Tài Khoản Test Có Sẵn

| Email | Mật khẩu | Vai trò | Tenant |
|-------|----------|---------|--------|
| `admin@pluginruntime.internal` | `admin` | Platform Admin | Platform Operations (Internal) |
| `alice@acme.com` | `secret` | Tenant Owner | Acme Corp (Gói Pro) |
| `bob@startuplabs.io` | `secret` | Tenant Owner | Startup Labs (Gói Free) |
| `carol@enterprise-global.com` | `secret` | Tenant Owner | Enterprise Global (Gói Enterprise) |

## Đăng Nhập Qua Web Portal

### Marketplace Portal (http://localhost:6300)

1. Mở http://localhost:6300
2. Nhấn **Login** ở thanh trên
3. Chọn nút Quick Login:
   - **Alice (Pro Tenant)** — developer có gói trả phí
   - **Bob (Free Tenant)** — developer gói miễn phí
   - **Admin User** — quản trị viên

### Consumer Portal (http://localhost:6400)

1. Mở http://localhost:6400
2. Nhấn **Login**
3. Chọn tài khoản test

## Sau Khi Đăng Nhập Bạn Có Thể Làm Gì

### Với tài khoản Alice (Gói Pro)
- Duyệt plugin trên Marketplace
- Tải lên extension mới
- Xem subscription
- Truy cập tối đa 10.000 request/ngày

### Với tài khoản Bob (Gói Free)
- Duyệt plugin
- Giới hạn 100 request/ngày
- Không thể đăng ký package (Gói Free)

### Với tài khoản Admin
- Xem tất cả tenant: `/api/admin/tenants`
- Danh sách gói: `/api/admin/plans`
- Duyệt/từ chối plugin
- Tạm ngưng tenant

## Dùng API Gateway

Gateway dùng API key (không phải JWT). Dùng key từ dữ liệu test:

```bash
# Với tư cách Acme Corp (Gói Pro)
curl http://localhost:6200/api/plugins ^
  -H "X-Api-Key: acme_pro_TeSt1234567890abcdefghijklmnop7k9"
```

## Swagger UI

Mở http://localhost:6100/swagger để xem và thử tất cả API endpoint.

Để xác thực trong Swagger:
1. Gọi `POST /api/auth/login` với thông tin test
2. Copy `token` từ response
3. Nhấn nút "Authorize" trong Swagger
4. Nhập: `Bearer <token-cua-ban>`
5. Mọi request tiếp theo sẽ được xác thực
