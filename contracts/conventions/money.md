# Quy ước: Tiền tệ

> Source: [ADR-0001](../../docs/adr/0001-tien-te-dung-decimal.md). File này là bản rút gọn để service tra cứu khi triển khai.

Áp dụng cho mọi service có xử lý tiền: Product, Cart, Order, Promotion, Payment, Worker.

## Quy tắc

| Tầng | Kiểu | Ví dụ |
|---|---|---|
| PostgreSQL | `NUMERIC(19,4)` | `19.9900` |
| C# / EF Core | `decimal` (hoặc `decimal?`) | `19.99m` |
| JSON (API request/response) | number thập phân | `19.99` |
| JSON (event payload) | number thập phân | `19.99` |

**Cấm dùng `float`, `double`, `real` cho bất kỳ giá trị tiền tệ nào.**

Không lưu dạng integer cents. Không serialize tiền thành string.

## EF Core

Khai báo precision trong `IEntityTypeConfiguration`, không dựa vào mặc định của provider:

```csharp
builder.Property(x => x.Price)
    .HasPrecision(19, 4)
    .IsRequired();
```

Tham khảo bản đã làm: `services/Product/Ecommerce.Product/Data/Configurations/ProductConfiguration.cs`.

## Event payload

Envelope (`../events/envelope.schema.json`) không ràng buộc kiểu bên trong `data`. Mỗi event schema có chở tiền phải tự khai báo đúng quy ước:

```json
{
  "unitPrice": {
    "type": "number",
    "description": "Đơn giá. NUMERIC(19,4). Không phải cents."
  }
}
```

Consumer deserialize sang `decimal`, không dùng `double`.

## Lưu ý cho client JavaScript

`JSON.parse("19.99")` trả về IEEE-754 double — cộng dồn nhiều dòng tiền ở frontend sẽ sinh sai số. Frontend nên:

- chỉ **hiển thị** giá trị backend gửi xuống, hoặc
- dùng thư viện decimal (`decimal.js`, `big.js`) nếu buộc phải tính toán phía client

Mọi con số cuối cùng có hiệu lực (tổng đơn, thuế, chiết khấu) phải do backend tính.

## Đơn vị tiền tệ (currency)

Mã tiền tệ dùng **ISO 4217, 3 ký tự viết hoa** (`VND`, `USD`). Cột `varchar(3)`, `IsRequired`, default `"VND"`.

Bản đã làm: `Product.Currency` — `Domain/Product.cs:21`, `ProductConfiguration.cs:20`, và có trong `CreateProductRequest` / `UpdateProductRequest`.

**Chưa quyết định** (cần ADR riêng khi thực sự hỗ trợ đa tiền tệ):

- `ProductVariant.Price` không có `Currency` riêng — hiện ngầm hiểu kế thừa từ Product
- Event payload xuyên service có bắt buộc chở `currency` cạnh mỗi số tiền hay không
- Quy đổi giữa các loại tiền
