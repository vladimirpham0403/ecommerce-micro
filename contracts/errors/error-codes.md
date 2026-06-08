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
| AUTH_TOKEN_EXPIRED | 401 | Access token hết hạn |
| AUTH_REFRESH_TOKEN_INVALID | 401 | Refresh token sai/đã dùng |
| AUTH_FORBIDDEN | 403 | Không đủ quyền |
| PRODUCT_NOT_FOUND | 404 | Không thấy product |
| PRODUCT_VARIANT_NOT_FOUND | 404 | Không thấy variant |
| PRODUCT_INACTIVE | 409 | Product ngừng bán |
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