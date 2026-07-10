# Hello JSON Extension (No Code)

Extension mẫu dạng JSON thuần — không cần biên dịch code.

## Cấu Trúc

```
hello-json-extension/
├── manifest.json      → Khai báo extension (processor type = json_template)
├── handler.json       → Logic xử lý: input schema, template, output mapping
└── README.md
```

## Cách Hoạt Động

Extension này dùng **JSON Template Processor** tích hợp sẵn trong platform:
1. Nhận input từ người gọi
2. Validate input theo `input_schema`
3. Chọn template greeting theo language
4. Thay thế `{{placeholders}}` bằng giá trị thực
5. Trả về output theo `output_template`

## Không Cần Build

Chỉ cần zip 2 file và upload:

```bash
zip hello-json.plugin.zip manifest.json handler.json
```

## Cách Gọi

```bash
# Tiếng Anh (mặc định)
curl -X POST https://gateway.example.com/api/plugins/execute \
  -H "X-Api-Key: your-key" \
  -H "Content-Type: application/json" \
  -d '{
    "extension_id": "com.samples.hello-json",
    "input": { "name": "Kiro", "language": "en" }
  }'

# Tiếng Việt
curl -X POST https://gateway.example.com/api/plugins/execute \
  -H "X-Api-Key: your-key" \
  -d '{"extension_id": "com.samples.hello-json", "input": {"name": "Bạn", "language": "vi"}}'
```

## Response Mẫu

```json
{
  "message": "Xin chào, Bạn! Chào mừng đến với Plugin Runtime.",
  "language": "vi",
  "extension": "com.samples.hello-json",
  "version": "1.0.0",
  "processed_at": "2024-07-09T10:30:00Z"
}
```

## Khi Nào Dùng JSON Extension

- Logic đơn giản: transform input → output
- Không cần gọi database, network, hay service khác
- Muốn deploy nhanh không cần build/compile
- Template-based responses (greeting, formatting, routing)
- Prototype / POC trước khi viết extension đầy đủ

## So Sánh Với C# Extension

| | C# Extension | JSON Extension |
|-|:---:|:---:|
| Cần biên dịch? | ✅ Có | ❌ Không |
| Dùng capability? | ✅ Database, Network, etc. | ❌ Chỉ transform |
| Logic phức tạp? | ✅ Tùy ý | ❌ Chỉ template |
| Tốc độ phát triển | Chậm hơn | Rất nhanh |
| Phù hợp cho | Business logic phức tạp | Transform đơn giản |
