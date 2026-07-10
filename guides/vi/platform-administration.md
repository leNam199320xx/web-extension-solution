# Hướng Dẫn Quản Trị Nền Tảng

Hướng dẫn này dành cho vận hành viên quản lý tenant, duyệt plugin và giữ hệ thống hoạt động tốt.

## Trách Nhiệm Của Bạn

Với vai trò quản trị viên, bạn:
- Xem xét và phê duyệt/từ chối plugin được tải lên
- Quản lý tài khoản tenant (tạo, tạm ngưng, kích hoạt lại, xóa)
- Cấu hình gói và giá
- Tạo và quản lý plugin package
- Giám sát sức khỏe hệ thống và mức sử dụng
- Xử lý sự cố bảo mật

## Truy Cập Admin Portal

Đăng nhập Admin Portal với tài khoản có role **Platform_Admin**.

## Quy Trình Duyệt Plugin

Khi nhà phát triển tải extension mới lên, nó vào hàng đợi duyệt.

### Xem Xét Plugin

Với mỗi plugin đang chờ, kiểm tra:

1. **Quyền yêu cầu** — Có hợp lý với chức năng plugin mô tả không?
2. **Lý do** — Nhà phát triển có giải thích vì sao cần mỗi quyền không?
3. **Kết quả quét bảo mật** — Quét tự động có tìm thấy vấn đề gì không?
4. **Giới hạn tài nguyên** — Giới hạn yêu cầu (bộ nhớ, CPU, thời gian) có phù hợp không?

### Hướng Dẫn Quyết Định

| Tình huống | Hành động |
|-----------|----------|
| Quyền phù hợp mô tả, quét sạch | Phê duyệt |
| Yêu cầu ghi database nhưng mô tả là "phân tích chỉ đọc" | Từ chối — quyền không khớp mục đích |
| Yêu cầu network không có lý do | Từ chối — yêu cầu giải thích |
| Quét phát hiện lỗ hổng | Từ chối — thông báo developer |
| Mọi thứ ổn nhưng giới hạn tài nguyên quá cao | Phê duyệt với giới hạn giảm |

### Khi Phê Duyệt

Nền tảng sẽ:
1. Ký manifest bằng private key
2. Lưu phiên bản đã ký
3. Extension khả dụng để thực thi
4. Thông báo cho developer

### Khi Từ Chối

1. Cung cấp lý do rõ ràng (developer sẽ thấy)
2. Gợi ý cách sửa nếu có thể
3. Developer có thể sửa và tải lại

## Quản Lý Tenant

### Tạo Internal Tenant

Internal tenant là tài khoản đặc biệt cho vận hành:
- Không có billing hay tích hợp Stripe
- Rate limit và quota không giới hạn
- API key và package subscription không giới hạn
- Chỉ Platform_Admin mới tạo được

### Tạm Ngưng Tenant

Dùng tạm ngưng khi:
- Thanh toán quá hạn 30+ ngày
- Phát hiện vi phạm điều khoản sử dụng
- Lo ngại bảo mật cần điều tra

Khi tạm ngưng:
- Tất cả API request từ tenant bị từ chối
- API key vẫn còn nhưng ngừng hoạt động
- Thông báo Redis gửi đến gateway ngay lập tức
- Audit log ghi lại ai tạm ngưng và vì sao

### Kích Hoạt Lại Tenant

Sau khi vấn đề được giải quyết:
- Kích hoạt lại từ Admin Portal
- Mọi truy cập phục hồi ngay lập tức
- Audit log ghi lại việc kích hoạt lại

## Quản Lý Plugin Package

### Tạo Package

1. Chọn tên và mô tả
2. Đặt giá hàng tháng
3. Chọn extension bao gồm
4. Tất cả extension phải tồn tại và ở trạng thái Active

### Cập Nhật Package

- Thêm/bớt extension bất kỳ lúc nào
- Khi plugin thay đổi, quyền truy cập của tất cả subscriber được tính lại tự động
- Thông báo Redis gửi đến gateway để thay đổi áp dụng ngay

### Vô Hiệu Hóa Package

- Đặt thành Inactive
- Subscriber hiện tại giữ quyền truy cập đến hết kỳ
- Không nhận đăng ký mới
- Package biến mất khỏi danh sách công khai

## Quản Lý API Key

### Thu Hồi Key

Nếu key bị lộ:
1. Thu hồi ngay qua Admin Portal hoặc API
2. Truyền qua Redis đến gateway trong vài giây
3. Mọi request dùng key đó bị từ chối ngay lập tức
4. Audit entry được tạo

## Giám Sát và Sức Khỏe

### Health Check

Endpoint `/health` kiểm tra:
- Kết nối PostgreSQL
- Kết nối Redis
- Trạng thái module (Plugins, Tenants, Billing, Subscriptions, Gateway)

Trả về `200 Healthy` hoặc `503 Unhealthy` với chi tiết lỗi.

### Metrics

Endpoint `/metrics` cung cấp dữ liệu Prometheus:
- Tổng request theo module
- Tỷ lệ lỗi theo module
- Độ trễ Stripe API
- Số package subscription đang hoạt động

## Xử Lý Sự Cố Bảo Mật

Nếu nghi ngờ có vấn đề bảo mật:

1. **Tạm ngưng tenant liên quan** — dừng mọi truy cập API ngay
2. **Thu hồi key bị lộ** — truyền đến gateway trong vài giây
3. **Kiểm tra audit log** — hiểu chuyện gì xảy ra và khi nào
4. **Xem xét extension** — nếu plugin liên quan, vô hiệu hóa nó
5. **Ghi chép mọi thứ** — cập nhật audit với phát hiện

## Tham Chiếu Nhanh

| Hành động | Vị trí | API Endpoint |
|-----------|--------|-------------|
| Danh sách tenant | Admin → Tenants | `GET /api/admin/tenants` |
| Tạm ngưng tenant | Admin → Chi tiết → Suspend | `POST /api/tenants/{id}/suspend` |
| Duyệt plugin | Admin → Plugin đang chờ → Approve | — |
| Danh sách gói | Admin → Plans | `GET /api/admin/plans` |
| Danh sách package | Admin → Packages | `GET /api/admin/packages` |
| Health check | — | `GET /health` |
| Metrics | — | `GET /metrics` |
