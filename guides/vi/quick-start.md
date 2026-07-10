# Bắt Đầu Nhanh — 5 Phút Để Chạy

Chọn vai trò của bạn và làm theo các bước bên dưới.

---

## Tôi là Nhà Phát Triển Plugin

**Mục tiêu:** Xuất bản extension đầu tiên lên nền tảng.

1. **Đăng ký** tài khoản trên Marketplace Portal
2. **Tạo manifest** — file JSON mô tả extension, capability và quyền
3. **Viết plugin** — implement `IPlugin` từ SDK, dùng capability qua `PluginContext`
4. **Đóng gói** — nén `manifest.json` + DLL thành `my-plugin.plugin.zip`
5. **Tải lên** — kéo thả vào trang Upload trên Marketplace Portal
6. **Chờ duyệt** — admin sẽ xem xét quyền bạn yêu cầu
7. **Xong!** — extension của bạn đã hoạt động và có thể gọi được

📖 Hướng dẫn đầy đủ: [Phát Triển Extension](extension-development.md)

---

## Tôi là Người Dùng API

**Mục tiêu:** Gọi extension thông qua API.

1. **Đăng ký** trên Consumer Portal — chọn gói (Free để bắt đầu)
2. **Sao chép API key** — chỉ hiển thị một lần duy nhất
3. **Gọi API:**
   ```bash
   curl https://gateway.example.com/api/plugins/execute \
     -H "X-Api-Key: api-key-cua-ban" \
     -d '{"extension_id": "com.example.hello", "input": {}}'
   ```
4. **Duyệt package** — đăng ký các gói plugin để truy cập extension cao cấp
5. **Theo dõi sử dụng** — xem dashboard để biết số request hàng ngày và quota

📖 Hướng dẫn đầy đủ: [Đăng Ký & Sử Dụng](subscription-and-usage.md)

---

## Tôi là Quản Trị Viên

**Mục tiêu:** Giữ cho nền tảng chạy ổn định.

1. **Đăng nhập** Admin Portal với tài khoản Platform_Admin
2. **Duyệt plugin mới** — xem quyền, kết quả quét bảo mật, phê duyệt hoặc từ chối
3. **Giám sát tenant** — xem tenant hoạt động, gói, và mức sử dụng
4. **Xử lý sự cố** — tạm ngưng tenant có vấn đề, thu hồi key bị lộ
5. **Quản lý package** — tạo gói plugin, đặt giá, vô hiệu hóa gói cũ

📖 Hướng dẫn đầy đủ: [Quản Trị Nền Tảng](platform-administration.md)

---

## Chạy Nền Tảng Trên Máy

```bash
# Cách 1: Toàn bộ hệ thống với Aspire (cần Docker)
cd src/Aspire/PluginRuntime.AppHost
dotnet run
# Mở https://localhost:15888 để xem Aspire dashboard

# Cách 2: Chỉ chạy API (dùng JSON storage, không cần database)
cd src/PluginRuntime.Api
dotnet run
# Đặt "DatabaseProvider": "Json" trong appsettings.json để chạy không cần DB
```

---

## Đọc Tiếp

| Muốn... | Đọc |
|---------|-----|
| Xây dựng extension | [Phát Triển Extension](extension-development.md) |
| Dùng extension qua API | [Đăng Ký & Sử Dụng](subscription-and-usage.md) |
| Quản trị nền tảng | [Quản Trị Nền Tảng](platform-administration.md) |
