# Event Names - danh sách chuẩn

Mọi service phải dùng đúng tên dưới đây. Thêm event mới -> cập nhật file này trước.

## Quy ước
- PascalCase, thì quá khứ (event = việc đã xảy ra): `OrderCreated`, không phải `CreateOrder`.
- Một topic Kafka có thể chứa nhiều event cùng aggregate.

## Danh sách tối thiểu

| Event | Source | Mô tả |
|---|---|---|
| UserRegistered | auth | User đăng ký xong |
| ProductCreated | product | Tạo product |
| ProductUpdated | product | Cập nhật product (Search nghe để reindex) |
| ProductDeleted | product | Xóa product |
| CartCheckedOut | cart | Giỏ hàng được checkout |
| PriceCalculated | promotion | Đã tính giá cuối |
| CouponRedeemed | promotion | Coupon được dùng |
| OrderCreated | order | Đơn được tạo (mở đầu saga) |
| StockReserved | inventory | Giữ tồn kho thành công |
| StockFailed | inventory | Giữ tồn kho thất bại |
| StockReleased | inventory | Trả lại tồn kho (compensation) |
| PaymentPending | payment | Thanh toán đang chờ |
| PaymentCompleted | payment | Thanh toán xong |
| PaymentFailed | payment | Thanh toán thất bại |
| OrderConfirmed | order | Đơn xác nhận (saga thành công) |
| OrderCancelled | order | Đơn bị hủy (saga bù trừ) |
| NotificationCreated | notification | Tạo thông báo |

## Versioning
- Breaking change (xóa/đổi field) -> tăng `eventVersion`, giữ consumer cũ chạy được trong giai đoạn chuyển tiếp.