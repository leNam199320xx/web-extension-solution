# Hello World Extension (C# Project)

Extension mẫu dạng project .NET — đơn giản nhất có thể.

## Cách Build

```bash
cd samples/HelloWorld.Extension
dotnet build
```

## Cách Đóng Gói

```bash
dotnet publish -o ./publish
# Copy manifest.json vào thư mục publish, sau đó zip:
# → hello-world.plugin.zip chứa manifest.json + HelloWorld.Extension.dll
```

## Cách Gọi

```bash
curl -X POST https://gateway.example.com/api/plugins/execute \
  -H "X-Api-Key: your-key" \
  -H "Content-Type: application/json" \
  -d '{
    "extension_id": "com.samples.hello-world",
    "input": { "name": "Kiro" }
  }'
```

## Response

```json
{
  "message": "Hello, Kiro! This is your first extension running on Plugin Runtime.",
  "timestamp": "2024-07-09T10:30:00Z",
  "extension": "com.samples.hello-world",
  "version": "1.0.0"
}
```

## Đặc Điểm

- Không yêu cầu capability nào
- Không cần database, network, hay cache
- Giới hạn tài nguyên tối thiểu (64 MB RAM, 5 giây, 10% CPU)
- Visibility: public — ai cũng gọi được
