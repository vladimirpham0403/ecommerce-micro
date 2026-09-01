# Chứng thư của Auth service

Thư mục này chứa `signing.pfx` và `encryption.pfx` mà Auth dùng để ký và mã hóa token.
**File `.pfx` không được commit** (xem `.gitignore`) - mỗi máy tự sinh.

## Sinh lần đầu

```bash
AUTH_CERT_PASSWORD=auth_cert_dev_password ./scripts/gen-auth-certs.sh
```

Mật khẩu phải khớp `AUTH_CERT_PASSWORD` trong `.env`.

## Vì sao hai cert riêng

| Cert | Dùng để | Ai cần phần công khai |
|---|---|---|
| `signing.pfx` | ký access token + id token | **mọi service**, lấy qua `/.well-known/jwks` |
| `encryption.pfx` | mã hóa authorization code + refresh token | không ai - chỉ Auth đọc |

Gộp làm một thì mỗi lần xoay khóa ký sẽ giết luôn toàn bộ refresh token đang lưu hành.

## Vì sao Docker không dùng development certificate

`AddDevelopmentSigningCertificate()` ghi vào X509Store bên trong container, mất khi rebuild image.
Khóa mới nghĩa là token cũ bị từ chối - và vì service phía sau cache JWKS tới 24h, lỗi còn kéo dài
sau đó. Mount PFX read-only tránh hẳn chuyện này, đồng thời đúng bằng cách production làm.

## Xoay khóa không downtime

`AuthCredentials` đọc theo pattern `signing*.pfx`, nên chỉ cần thả thêm file:

1. Sinh `signing-2027.pfx` với hạn xa hơn, đặt cạnh `signing.pfx`
2. Restart Auth - OpenIddict ký bằng cert có `NotAfter` xa nhất, JWKS phát **cả hai** public key
3. Token cũ vẫn verify được cho tới khi hết hạn (15 phút)
4. Hôm sau xóa file cũ
