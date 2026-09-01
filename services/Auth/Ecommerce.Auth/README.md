# Auth Service

OIDC/OAuth2 provider dựng bằng **OpenIddict 7.6**. Các service khác verify token bằng public key
lấy từ JWKS - không service nào giữ secret chung.

Test bằng Postman, Swagger UI (`/swagger`) hoặc file [`Ecommerce.Auth.http`](Ecommerce.Auth.http).

> **Về trang đăng nhập.** Service này có một trang đăng nhập tối giản (`Pages/Account/Login`).
> Nó **không** phải frontend của ứng dụng mà là giao diện hạ tầng của chính OIDC provider - cùng
> nhóm với Swagger UI và Kafka UI. Không có nó thì `authorization_code` không tồn tại được, vì
> flow đó dựa trên việc người dùng gõ mật khẩu tại một trang thuộc về authorization server.
> Dự án vẫn là backend-only: không có SPA, không có npm, không có giao diện mua sắm.

## Endpoint

| Đường dẫn | Vai trò                                                                        |
|---|--------------------------------------------------------------------------------|
| `/.well-known/openid-configuration` | discovery                                                                      |
| `/.well-known/jwks` | public key để verify chữ ký                                                    |
| `/connect/authorize` | bước 1 của Authorization Code + PKCE                                           |
| `/connect/token` | cấp token (authorization_code / password / refresh_token / client_credentials) |
| `/connect/logout` | end-session                                                                    |
| `/connect/userinfo` | claim của token hiện tại, JSON trần theo chuẩn OIDC                            |
| `/connect/revoke` | thu hồi token                                                                  |
| `/connect/introspect` | tra cứu token                                                                  |
| `/v1/auth/register` | đăng ký tài khoản                                                              |
| `/v1/auth/me` | thông tin từ token đang gửi, bọc `ApiResponse`                                 |
| `/v1/auth/users` | **quản trị:** danh sách người dùng (lọc `?search=`, phân trang)                |
| `/v1/auth/users/{id}/roles` | **quản trị:** gán lại role (PUT)                                               |
| `/v1/auth/sessions` | phiên đăng nhập của chính mình: xem (GET), đăng xuất mọi nơi (DELETE)          |
| `/v1/auth/sessions/{id}` | đăng xuất một thiết bị (DELETE)                                                |
| `/health` · `/ready` | liveness · readiness                                                           |

## Grant được bật

Bốn grant, chạy song song với mục đích:

| Grant | Dùng khi | Cần trình duyệt |
|---|---|---|
| `authorization_code` + PKCE | app người dùng cuối - **luồng chuẩn** | có |
| `password` | Postman, Swagger, script nội bộ - đường tắt | không |
| `refresh_token` | gia hạn, có rotation + phát hiện tái sử dụng | không |
| `client_credentials` | service-to-service (`service-worker`) | không |

### Vì sao giữ cả `authorization_code` lẫn `password`

`authorization_code` + PKCE giữ lại những thứ mà `password` **không bao giờ** làm được, vì chúng
đều cần một bước tương tác với người dùng:

- **2FA/OTP** - một form POST duy nhất không có chỗ hỏi mã thứ hai
- **Đăng nhập bằng Google/Facebook** - cắm external provider vào authorization endpoint
- **SSO** giữa nhiều ứng dụng
- **Client bên thứ ba** tích hợp mà không cầm mật khẩu người dùng

`password` (ROPC) **đã bị OAuth 2.1 loại bỏ** vì client phải cầm mật khẩu thật. Ở đây nó chỉ tồn tại như đường tắt cho client first-party, đổi lấy việc lấy token bằng đúng một request.

**Siết về đúng OAuth 2.1 khi lên production:** bỏ dòng `Permissions.GrantTypes.Password` khỏi
`web-client` trong `Data/AuthSeeder.cs`. Không phải sửa code, và `CreateOrUpdateAsync` sẽ tự cập
nhật client đã tồn tại trong database ở lần khởi động kế tiếp.

## Quản trị người dùng

`/v1/auth/register` luôn gán cứng role `Customer`. Nâng quyền cho ai đó đi qua `/v1/auth/users`:

```bash
# 1. lấy token có kèm scope users.manage
curl -X POST http://auth:5044/connect/token \
  -d 'grant_type=password&client_id=web-client&username=admin@ecom.local&password=Admin@123456&scope=openid roles users.manage'

# 2. tìm id
curl 'http://auth:5044/v1/auth/users?search=someone@ecom.test' -H "Authorization: Bearer $TOKEN"

# 3. gán lại toàn bộ role (thay thế, không cộng dồn)
curl -X PUT http://auth:5044/v1/auth/users/{id}/roles \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"roles":["Admin","Customer"]}'
```

Cần **đủ cả hai**: role `Admin` và scope `users.manage`. Role trả lời "ai", scope trả lời "token
này được phép làm gì" - một quản trị viên đăng nhập bằng app chỉ xin `product.read` thì token đó
không đổi được quyền của người khác.

Hai điểm đáng biết:

- **Không tự gỡ được role `Admin` của chính mình** (`AUTH_CANNOT_DEMOTE_SELF`). Không chặn thì một
  quản trị viên duy nhất bấm nhầm là cả hệ thống hết đường quản trị, chỉ còn cách vào psql sửa tay.
- **Đổi role không cắt token đang lưu hành.** Access token là JWT self-contained nên vẫn mang role
  cũ tối đa 15 phút; lần refresh kế tiếp mới lấy role mới (nhánh `refresh_token` dựng lại identity
  từ DB). Muốn cắt ngay thì thu hồi phiên của người đó.

## Phiên đăng nhập

Một phiên = một authorization của OpenIddict, gắn với cặp (người dùng, client, bộ scope). Refresh
token treo vào đó, nên thu hồi một phiên là cắt cả chuỗi refresh mà không cần biết từng token.

```bash
curl http://auth:5044/v1/auth/sessions -H "Authorization: Bearer $TOKEN"
curl -X DELETE http://auth:5044/v1/auth/sessions -H "Authorization: Bearer $TOKEN"   # mọi nơi
```

Không cần scope riêng - ai cũng xem và cắt được phiên của **chính mình**; mọi thao tác lọc theo
`sub` của token đang gửi chứ không nhận userId từ client. Phiên của người khác trả **404** chứ
không phải 403, vì 403 đã là xác nhận "id này có thật".

Giới hạn cần biết: sau khi thu hồi, access token vẫn dùng được tới lúc hết hạn (tối đa 15 phút).
Cắt ngay lập tức đòi introspection bắt buộc ở mọi service - đắt hơn nhiều thứ nó mua được.

## Khóa

Bốn thứ khác nhau, đừng gộp:

| Khóa | Loại | Ai giữ bí mật | Ai cần công khai |
|---|---|---|---|
| signing | RSA | chỉ Auth | **mọi service**, qua JWKS |
| encryption | RSA | chỉ Auth | không ai |
| Data Protection key ring | đối xứng | chỉ Auth | không ai |
| client secret | đối xứng | Auth + client đó | không ai |

Cách chọn khóa theo môi trường nằm ở `Common/AuthCredentials.cs`:

1. `Auth:Certificates:Ephemeral=true` → khóa trong RAM (dùng cho test)
2. `Auth:Certificates:Path` có giá trị → đọc `signing*.pfx` + `encryption*.pfx` (Docker và production)
3. Development → `AddDevelopment*Certificate()`, lưu vào X509Store của user
4. Còn lại → **ném exception**, cố tình không có nhánh tự sinh khóa ở production

**Cảnh báo hết hạn.** `/ready` có check `signing-certificate`: `Degraded` khi cert ký còn dưới
`Auth:Certificates:ExpiryWarningDays` (mặc định 30) ngày, `Unhealthy` khi đã hết hạn. Nó lấy hạn
**xa nhất** trong các cert đang nạp, vì OpenIddict ký bằng cert có `NotAfter` xa nhất - lấy hạn gần
nhất sẽ kêu inh ỏi đúng lúc rollover đang diễn ra đúng quy trình. Docker healthcheck gọi `/health`
(không chạy check nào) nên cảnh báo này không gây restart loop.

Xem `infra/certs/README.md` để sinh cert và để biết cách xoay khóa không downtime.

## Dọn token định kỳ

`TokenPruningService` chạy nền, gọi `PruneAsync` trên token rồi tới authorization (đúng thứ tự
đó - OpenIddict không xóa authorization còn token treo vào).

| Config | Mặc định | Ý nghĩa |
|---|---|---|
| `Auth:Pruning:Enabled` | `true` | tắt là hai bảng của OpenIddict lớn vô hạn |
| `Auth:Pruning:IntervalHours` | `12` | khoảng cách giữa hai lượt |
| `Auth:Pruning:RetentionDays` | `14` | khoảng lùi giữ lại để còn điều tra sự cố |

Không có bước này thì `OpenIddictTokens` chỉ lớn lên: mỗi lần đăng nhập sinh một authorization
code, mỗi lần refresh rotation sinh thêm một dòng và chỉ **đánh dấu** dòng cũ là redeemed.

## Sau reverse proxy

Rate-limit phân vùng theo IP người gọi. Khi Gateway (Phase 1.5) đứng trước, mọi request tới đây
mang IP của Gateway, nên **toàn bộ** người dùng sẽ dùng chung một hạn mức nếu không đọc
`X-Forwarded-For`:

```jsonc
"Auth": {
  "ForwardedHeaders": {
    "Enabled": true,
    "KnownNetworks": ["172.16.0.0/12"],   // dải mạng của proxy, dạng CIDR
    "ForwardLimit": 1                      // số chặng proxy tin được
  }
}
```

Mặc định **tắt**, và bật mà không khai `KnownNetworks`/`KnownProxies` thì host **không khởi động**.
Cố ý như vậy: tin `X-Forwarded-For` khi không có proxy thật đứng trước đồng nghĩa với cho client tự
khai IP, tức là tự tay mở đường vượt rate-limit.

## Lấy token

**Nhanh nhất - password grant, một request:**

```bash
curl -X POST http://auth:5044/connect/token \
  -d 'grant_type=password&client_id=web-client&username=admin@ecom.local&password=Admin@123456&scope=openid email profile roles offline_access product.read product.write'
```

**Swagger UI:** bấm **Authorize**, chọn một trong hai scheme:

- `oauth2-password` - nhập email + mật khẩu ngay trong Swagger
- `oauth2-authorization-code` - Swagger mở trang đăng nhập, tự sinh PKCE, tự đổi code lấy token

**Postman, luồng chuẩn:** tab Authorization → Type `OAuth 2.0` → Grant Type
`Authorization Code (With PKCE)`, Auth URL `http://auth:5044/connect/authorize`, Access Token URL
`http://auth:5044/connect/token`, Client ID `web-client`, Callback URL
`https://oauth.pstmn.io/v1/callback` (đã đăng ký sẵn). Bấm **Get New Access Token** - Postman tự
mở trình duyệt cho bạn đăng nhập rồi đổi code.

## Điểm dễ gặp lỗi

- **Issuer phải giống nhau trong và ngoài Docker.** Xem README ở repo root về hosts file.
- **`DisableTransportSecurityRequirement()`** được bật khi issuer là `http://`. Production khai
  `https://` thì yêu cầu HTTPS tự động được giữ lại.
- **Không xin `offline_access` thì không có refresh token.** Đây là hành vi đúng chuẩn, không phải lỗi.
- **Refresh token reuse leeway = 0.** OpenIddict mặc định cho một cửa sổ khoan dung để client
  retry; Phase 1 tắt nó để phát hiện tái sử dụng chặt chẽ. Chỉnh qua
  `Auth:RefreshTokenReuseLeewaySeconds` nếu client thật gặp lỗi do retry.
- **`CreateAsync(descriptor)` của authorization manager KHÔNG tự điền `CreationDate`.** Bỏ sót thì
  `PruneAsync` (lọc theo `CreationDate < threshold`) không bao giờ khớp và bản ghi nằm lại vĩnh
  viễn. `AuthorizationController` đặt tay trường này.
- **Migration gồm cả 4 bảng của OpenIddict.** Nếu container crash loop vì `42P07 relation already
  exists`, `DROP DATABASE auth_db` rồi up lại - **không** dùng `make clean` (xóa hết volume).

## Test

```bash
dotnet test tests/Ecommerce.Auth.Tests
```

Testcontainers tự dựng Postgres. Mọi request trong test đều tái hiện được y hệt bằng Postman.
