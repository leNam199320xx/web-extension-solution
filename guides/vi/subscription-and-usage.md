# Hướng Dẫn Đăng Ký & Sử Dụng Extension

Hướng dẫn này giải thích cách đăng ký và sử dụng extension trên nền tảng Plugin Runtime.

## Cách Truy Cập Hoạt Động

Không phải tất cả extension đều miễn phí. Nền tảng có 3 cấp độ truy cập:

| Cấp độ | Ai dùng được | Cách lấy quyền |
|--------|-------------|----------------|
| **Free** | Tất cả mọi người | Tự động — không cần làm gì |
| **Package** | Người đăng ký gói | Đăng ký plugin package chứa extension đó |
| **Subscription** | Người được phê duyệt | Gửi yêu cầu đăng ký cho chủ extension |

## Bắt Đầu Với Vai Trò Người Dùng API

### Bước 1: Đăng Ký Tài Khoản

Đăng ký qua Consumer Portal. Chọn gói:

| Gói | Giá/tháng | Request/ngày | API Keys | Gói đăng ký |
|-----|:---------:|:---:|:---:|:---:|
| Free | $0 | 100 | 2 | 0 |
| Pro | $49 | 10.000 | 10 | 5 |
| Enterprise | $299 | Không giới hạn | 50 | Không giới hạn |

### Bước 2: Lấy API Key

Sau khi đăng ký, hệ thống tạo API key đầu tiên. Key này chỉ hiển thị MỘT LẦN — sao chép ngay.

```
API Key của bạn: acme_pro_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx7k9

⚠️ Key này sẽ không hiển thị lại. Lưu trữ an toàn.
```

### Bước 3: Gọi API Đầu Tiên

```bash
curl https://gateway.example.com/api/plugins/execute \
  -H "X-Api-Key: api-key-cua-ban" \
  -H "Content-Type: application/json" \
  -d '{"extension_id": "com.example.hello-world", "input": {}}'
```

## Đăng Ký Plugin Package

Plugin package là gói extension được tuyển chọn, có phí hàng tháng.

### Gói Có Gì?

Mỗi gói chứa một nhóm extension liên quan. Ví dụ:

- **Analytics Suite** ($19,99/tháng) — Dashboard realtime, tổng hợp dữ liệu, công cụ báo cáo
- **Security Toolkit** ($29,99/tháng) — Quét lỗ hổng, phát hiện mối đe dọa, báo cáo tuân thủ
- **Developer Tools** ($14,99/tháng) — Sinh code, tiện ích test, tích hợp CI/CD

### Cách Đăng Ký

**Qua Consumer Portal:**
1. Vào Consumer Portal → Plans
2. Duyệt các gói có sẵn
3. Nhấn "Subscribe" vào gói muốn dùng
4. Xác nhận — thanh toán bắt đầu ngay

**Qua API:**
```bash
curl -X POST https://api.example.com/api/subscriptions/packages/{packageId}/subscribe \
  -H "Authorization: Bearer <token>"
```

### Sau Khi Đăng Ký

- Tất cả extension trong gói khả dụng ngay lập tức
- Danh sách truy cập plugin được cập nhật realtime
- Phí hàng tháng được cộng vào hóa đơn tiếp theo
- Bạn có thể gọi bất kỳ extension nào trong gói bằng API key

### Hủy Đăng Ký

- Có thể hủy bất kỳ lúc nào
- Quyền truy cập tiếp tục đến hết kỳ thanh toán hiện tại
- Không hoàn tiền cho tháng dở

## Đăng Ký Extension-to-Extension

Một số extension có visibility **Subscription** — nghĩa là extension của bạn cần được chủ sở hữu phê duyệt trước khi gọi được.

### Tại Sao Có Cơ Chế Này

Chủ extension kiểm soát ai được gọi extension của họ. Điều này cho phép:
- Extension trả phí xác minh người đăng ký
- API riêng giới hạn truy cập cho đối tác tin cậy
- Dịch vụ nhạy cảm tốc độ quản lý lượng caller

### Gửi Yêu Cầu Đăng Ký

**Qua Marketplace Portal:**
1. Tìm extension trên Marketplace
2. Nhấn "Request Subscription"
3. Điền:
   - **Lý do** — Tại sao bạn cần quyền truy cập
   - **Mức sử dụng dự kiến** — Bao nhiêu lượt gọi/ngày, mẫu sử dụng
4. Gửi yêu cầu

**Qua API:**
```bash
curl -X POST https://api.example.com/api/subscriptions \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "targetExtensionId": "com.partner.enrichment-service",
    "reason": "Cần làm giàu dữ liệu cho pipeline phân tích",
    "expectedUsage": {
      "requestsPerDay": 500,
      "usagePattern": "Xử lý batch mỗi giờ"
    }
  }'
```

### Quy Trình Phê Duyệt

Sau khi gửi yêu cầu:

1. Chủ extension nhận thông báo
2. Họ xem lý do và mức sử dụng dự kiến
3. Họ có thể:
   - **Phê duyệt** — Tùy chọn kèm điều kiện hoặc ngày hết hạn
   - **Từ chối** — Kèm lý do giải thích

Bạn sẽ nhận thông báo kết quả. Nếu được phê duyệt, có thể gọi extension ngay.

## Giới Hạn Gói và Quota

### Giới Hạn Đăng Ký Package

Gói của bạn giới hạn số package được đăng ký:
- **Free** — Không thể đăng ký package nào
- **Pro** — Tối đa 5 package đang hoạt động
- **Enterprise** — Không giới hạn

### Quota Request Hàng Ngày

Mỗi gói có giới hạn tổng request/ngày:
- **Free** — 100 request/ngày
- **Pro** — 10.000 request/ngày
- **Enterprise** — Không giới hạn

Khi vượt quota, request trả về `429 Too Many Requests` với header `Retry-After` cho biết quota reset lúc nào (nửa đêm UTC).

### Rate Limit

Ngoài quota hàng ngày, có rate limit theo phút:
- **Free** — 100 request/phút
- **Pro** — 10.000 request/phút
- **Enterprise** — Không giới hạn

### Phí Vượt Mức (Overage)

Với gói Pro, nếu vượt quota hàng ngày:
- $0,50 cho mỗi 1.000 request vượt mức
- Phí vượt mức xuất hiện trên hóa đơn tháng

## Thanh Toán và Hóa Đơn

### Chi Tiết Hóa Đơn Tháng

Hóa đơn tháng gồm:
1. **Phí gói cơ bản** — Giá tháng của gói
2. **Phí vượt mức** — Request vượt quota (chỉ gói Pro)
3. **Đăng ký package** — Tổng phí các package đang dùng

Ví dụ:
```
Gói (Pro):                          $49,00
Vượt mức (2.500 thừa × $0,50/1k):   $1,25
Gói Analytics Suite:                $19,99
Gói Security Toolkit:               $29,99
─────────────────────────────────────────
Tổng:                              $100,23
```

### Thanh Toán

- Thanh toán tự động qua Stripe
- Thanh toán thất bại → tài khoản bị tạm ngưng sau 30 ngày
- Quản lý phương thức thanh toán: Consumer Portal → Billing → Manage Payment

## Nâng/Hạ Cấp Gói

### Nâng cấp (có hiệu lực ngay)

- Giới hạn mới áp dụng ngay
- Tính phí chênh lệch cho phần còn lại của kỳ thanh toán

### Hạ cấp (có hiệu lực kỳ tiếp theo)

- Giới hạn hiện tại giữ nguyên đến ngày thanh toán tiếp
- Hệ thống kiểm tra bạn không vượt giới hạn gói mới:
  - Quá nhiều API key? Hủy bớt trước
  - Quá nhiều package subscription? Hủy đăng ký bớt

## Theo Dõi Sử Dụng

### Dashboard

Dashboard hiển thị:
- Số request hôm nay so với quota
- API key đang hoạt động và sắp hết hạn
- Hoạt động gần đây (5 ngày gần nhất)
- Gói hiện tại và trạng thái đăng ký

### Phân Tích Sử Dụng

Vào Usage Analytics để xem biểu đồ chi tiết:
- Request hàng ngày theo thời gian (có đường quota)
- Tỷ lệ thành công theo ngày
- Thời gian phản hồi trung bình
- Lọc theo khoảng thời gian (tối đa 90 ngày)

### Cảnh Báo Quota

Khi sử dụng hàng ngày vượt 80% quota, bạn sẽ thấy badge cảnh báo trên dashboard.

## Tham Chiếu Nhanh

| Hành động | Ở đâu |
|-----------|-------|
| Đăng ký package | Consumer Portal → Plans (`http://localhost:6400/plans`) |
| Yêu cầu truy cập extension | Marketplace → Chi tiết Extension → Request Subscription (`http://localhost:6300`) |
| Xem trạng thái đăng ký | Marketplace → My Subscriptions |
| Xem sử dụng | Consumer Portal → Usage Analytics (`http://localhost:6400/usage`) |
| Quản lý API key | Consumer Portal → API Keys (`http://localhost:6400/api-keys`) |
| Xem hóa đơn | Consumer Portal → Billing (`http://localhost:6400/billing`) |
| Đổi gói | Consumer Portal → Plans |
| Quản lý thanh toán | Consumer Portal → Billing → Manage Payment |
| Admin Portal | `http://localhost:6500` |
| API Swagger | `http://localhost:6100/swagger` |
| Aspire Dashboard | `http://localhost:6000` |
