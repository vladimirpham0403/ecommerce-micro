# ADR-0001 — Tiền tệ lưu bằng `decimal` / `NUMERIC(19,4)`

- **Status:** Accepted
- **Ngày:** 2026-07-28
- **Phạm vi:** toàn hệ thống — mọi service có xử lý tiền (Product, Cart, Order, Promotion, Payment)

## Bối cảnh

`docs/architecture.md` (Phase 0) ban đầu quy định "giá lưu bằng integer (cents)". Nhưng code Product service đã hiện thực bằng `decimal`:

| Vị trí | Kiểu |
|---|---|
| `services/Product/Ecommerce.Product/Domain/Product.cs:19` | `decimal Price` |
| `services/Product/Ecommerce.Product/Domain/ProductVariant.cs:16` | `decimal? Price` |
| Migration `20260620144012_InitialCreate.cs:67,101` | `numeric(19,4)`, precision 19, scale 4 |

Hai nguồn tài liệu nói ngược nhau. Cần chốt một hướng **trước khi** có service thứ hai đọc giá — từ Phase 5 giá sẽ đi xuyên service qua Kafka event, lúc đó đổi kiểu dữ liệu đồng nghĩa với sửa contract đã publish.

Ba lựa chọn được cân nhắc:

1. **Integer cents** — lưu số nguyên đơn vị nhỏ nhất (vd 19.99 → `1999`)
2. **`decimal` / `NUMERIC(p,s)`** — kiểu thập phân chính xác của Postgres
3. `float` / `double` — loại ngay, sai số dấu phẩy động không chấp nhận được với tiền

## Quyết định

**Dùng `decimal` trong .NET, map sang `NUMERIC(19,4)` trong PostgreSQL.** Giữ nguyên code hiện tại; sửa `architecture.md` cho khớp.

Quy ước cụ thể:

- Cột tiền: `NUMERIC(19,4)` — 19 chữ số tổng, 4 chữ số thập phân
- Thuộc tính C#: `decimal` (hoặc `decimal?` nếu nullable)
- JSON trên API: số thập phân thường (`19.99`), **không** phải string, **không** phải cents
- Cấm dùng `float`/`double`/`real` cho bất kỳ giá trị tiền tệ nào

## Lý do

**`NUMERIC` là kiểu chuẩn cho tiền tệ trong PostgreSQL.** Nó là số thập phân chính xác tuyệt đối, không có sai số dấu phẩy động, và map thẳng 1-1 sang `decimal` của .NET — không cần lớp chuyển đổi nào ở giữa.

**Lập luận "integer cents" chủ yếu đến từ ràng buộc của hệ sinh thái khác.** Nó phổ biến vì JavaScript/JSON không có kiểu decimal thật (`Number` là IEEE-754 double), nên buộc phải dùng integer để tránh sai số. Stack .NET + PostgreSQL không vướng hạn chế đó — cả hai đầu đều có decimal chuẩn.

**Integer cents đẩy gánh nặng quy đổi ra mọi biên hệ thống.** Mỗi lần đọc/ghi ở API, UI, báo cáo, export đều phải nhân/chia 100. Mỗi chỗ quên là một bug sai giá 100 lần — loại bug im lặng, khó phát hiện, và tốn kém khi lọt ra production.

**Scale 4 dự phòng cho đơn giá lẻ hơn đơn vị nhỏ nhất.** Giá nhập theo lô, tỷ giá, phân bổ chiết khấu theo tỷ lệ, thuế suất — đều có thể sinh ra phần thập phân nhỏ hơn cent. Integer cents không biểu diễn được nhóm này mà không thêm một quy ước phụ.

**Không hợp với VND nếu dùng cents.** Đồng Việt Nam không có đơn vị phụ đang lưu hành, nên "cents" là khái niệm vay mượn không tự nhiên với thị trường chính của dự án.

## Hệ quả

**Tích cực**

- Code hiện tại giữ nguyên, không cần migration mới
- Không có tầng quy đổi → ít bề mặt lỗi
- Query SQL trực tiếp đọc ra giá đúng dạng người đọc hiểu ngay, tiện debug và làm báo cáo

**Tiêu cực / cần lưu ý**

- **Client JavaScript vẫn phải cẩn thận.** `JSON.parse` sẽ đưa `19.99` về double. Frontend không được cộng dồn tiền bằng số thường — phải dùng thư viện decimal, hoặc chỉ hiển thị còn mọi phép tính đều do backend làm. Đây là ràng buộc thật, không né được bằng cách chọn kiểu ở backend.
- **Phải nhất quán scale khi tiền đi qua event.** Envelope event (`contracts/events/envelope.schema.json`) chở `data` tự do — mỗi event schema có tiền phải khai báo đúng quy ước này.
- **`decimal` chậm hơn integer** trong tính toán thuần. Không đáng kể ở quy mô dự án này, nhưng cần biết nếu sau này có batch tính toán lớn.
- **Đơn vị tiền tệ đã có sẵn, nhưng chưa có quy tắc dùng.** `Product.Currency` là `varchar(3)`, `IsRequired`, default `"VND"` (`Domain/Product.cs:21`, `ProductConfiguration.cs:20`), và có mặt trong `CreateProductRequest` / `UpdateProductRequest`. Nghĩa là số tiền trong hệ thống luôn đi kèm mã tiền tệ ở mức Product. Những gì **chưa** được quyết định: (a) `ProductVariant.Price` không có `Currency` riêng — ngầm hiểu kế thừa từ Product, chưa ghi thành quy tắc; (b) event payload xuyên service có bắt buộc chở `currency` cạnh mỗi số tiền không; (c) quy đổi đa tiền tệ. Cần ADR riêng khi thực sự hỗ trợ nhiều loại tiền.

## Tham chiếu

- `contracts/conventions/money.md` — quy ước chi tiết cho các service
- `docs/architecture.md` Phase 0 — dòng quy định kiểu giá
- `.scratch/phase-0-completion/PRD.md` hạng mục 3 — bối cảnh phát hiện mâu thuẫn
