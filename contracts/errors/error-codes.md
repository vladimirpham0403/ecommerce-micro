# API Response & Error Codes — chuẩn dùng chung

## Success response
```json
{
  "success": true,
  "data": {},
  "meta": { "requestId": "uuid", "correlationId": "uuid" }
}
```

## Error response
```json
{
  "success": false,
  "error": { "code": "ORDER_NOT_FOUND", "message": "Order not found", "details": {} },
  "meta": { "requestId": "uuid", "correlationId": "uuid" }
}
```

## Quy ước
- `code`: UPPER_SNAKE_CASE, có tiền tố domain (`AUTH_`, `ORDER_`...).
- `message`: tiếng Anh, ngắn, an toàn để hiển thị cho client.
- `details`: tùy chọn, chứa field lỗi cụ thể (validation...).
- HTTP status đi kèm hợp lý: 400/401/403/404/409/422/500/503.

## Bảng error code tối thiểu

| Code | HTTP | Ý nghĩa |
|---|---|---|
| AUTH_INVALID_CREDENTIALS | 401 | Sai email/mật khẩu |
| AUTH_UNAUTHENTICATED | 401 | Thiếu token hoặc token không hợp lệ |
| AUTH_TOKEN_EXPIRED | 401 | Access token hết hạn |
| AUTH_REFRESH_TOKEN_INVALID | 401 | Refresh token sai/đã dùng |
| AUTH_FORBIDDEN | 403 | Không đủ quyền (sai role hoặc thiếu scope) |
| AUTH_EMAIL_ALREADY_USED | 409 | Email đã được đăng ký |
| AUTH_ACCOUNT_LOCKED | 400 | Tài khoản bị khóa tạm thời do sai mật khẩu nhiều lần |
| AUTH_USER_NOT_FOUND | 404 | Không có người dùng với id này |
| AUTH_ROLE_NOT_FOUND | 400 | Tên role không nằm trong danh sách hợp lệ |
| AUTH_CANNOT_DEMOTE_SELF | 400 | Tự gỡ role Admin của chính mình - bị chặn để không khóa hết đường quản trị |
| AUTH_SESSION_NOT_FOUND | 404 | Phiên không tồn tại, hoặc thuộc về người khác |
| PRODUCT_NOT_FOUND | 404 | Không thấy product |
| PRODUCT_VARIANT_NOT_FOUND | 404 | Không thấy variant |
| PRODUCT_INACTIVE | 409 | Product ngừng bán |
| VALIDATION_ERROR | 400 | Request không hợp lệ — chi tiết từng field trong `details` |
| SEARCH_QUERY_INVALID | 400 | Query search sai |
| CART_EMPTY | 409 | Giỏ rỗng |
| CART_ITEM_NOT_FOUND | 404 | Không thấy item trong giỏ |
| CART_INVALID_QUANTITY | 400 | Số lượng không hợp lệ |
| PROMO_COUPON_INVALID | 400 | Coupon sai |
| PROMO_COUPON_EXPIRED | 409 | Coupon hết hạn |
| PROMO_COUPON_EXHAUSTED | 409 | Coupon hết lượt |
| ORDER_NOT_FOUND | 404 | Không thấy đơn |
| ORDER_INVALID_STATUS | 409 | Trạng thái đơn không cho phép thao tác |
| ORDER_ALREADY_CANCELLED | 409 | Đơn đã hủy |
| ORDER_DUPLICATED_REQUEST | 409 | Request trùng (idempotency) |
| INVENTORY_NOT_ENOUGH_STOCK | 409 | Không đủ tồn kho |
| INVENTORY_RESERVATION_EXPIRED | 409 | Reservation hết hạn |
| PAYMENT_FAILED | 402 | Thanh toán thất bại |
| PAYMENT_TIMEOUT | 504 | Thanh toán quá thời gian |
| PAYMENT_ALREADY_PROCESSED | 409 | Đã xử lý thanh toán rồi |
| BROKER_PUBLISH_FAILED | 500 | Publish event lỗi |
| BROKER_CONSUME_FAILED | 500 | Consume event lỗi |
| SYSTEM_INTERNAL_ERROR | 500 | Lỗi nội bộ |
| SYSTEM_TIMEOUT | 504 | Quá thời gian |
| SYSTEM_SERVICE_UNAVAILABLE | 503 | Service không sẵn sàng |
| SYSTEM_TOO_MANY_REQUESTS | 429 | Vượt rate limit |

## Lưu ý về lỗi của endpoint OIDC

`/connect/*` của Auth **không** dùng envelope trên. Chúng trả đúng định dạng của OAuth 2.0
(`{"error":"invalid_grant","error_description":"..."}`) vì client OIDC chuẩn mong đợi như vậy.
Chỉ REST API (`/v1/**`) mới theo `ApiResponse`.