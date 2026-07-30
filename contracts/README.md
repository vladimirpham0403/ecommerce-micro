# Contracts - Chuẩn dùng chung giữa các service

Thư mục này định nghĩa các **quy chuẩn dùng chung giữa các service** trong hệ thống.

> Nguyên tắc: **cập nhật tài liệu chuẩn trước, sau đó mới sửa code service**.

## Cấu trúc thư mục

- `openapi/` - OpenAPI spec của từng service.
- `events/` - Chuẩn event envelope và danh sách tên event.
- `proto/` - gRPC proto cho sync call nội bộ (vd Cart → Product).
- `errors/` - Danh sách error code và format response lỗi.
- `conventions/` - Quy ước kiểu dữ liệu dùng chung (tiền tệ, thời gian...).

## Nguyên tắc

- Contract-first: không service nào tự đặt format request, response, error hoặc event riêng.
- Mọi event phải tuân theo `events/envelope.schema.json`.
- Mọi API phải trả lỗi đúng format trong `errors/error-codes.md`.
- Mọi giá trị tiền tệ phải theo `conventions/money.md` (`decimal` / `NUMERIC(19,4)`).