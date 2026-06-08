# E-commerce Microservices — Roadmap ĐẦY ĐỦ (bản hoàn chỉnh)

---

## MỤC LỤC

1. [Phân bổ ngôn ngữ & service](#1-phân-bổ-ngôn-ngữ--service)
2. [Tech stack](#2-tech-stack)
3. [Sơ đồ kiến trúc đầy đủ](#3-sơ-đồ-kiến-trúc-đầy-đủ)
4. [Cấu trúc thư mục đích](#4-cấu-trúc-thư-mục-đích)
5. [Nguyên tắc vàng](#5-nguyên-tắc-vàng)
6. [Roadmap chi tiết (Phase -1 → 13)](#6-roadmap-chi-tiết)
7. [Event contract](#7-event-contract-chuẩn)
8. [Database per service](#8-database-per-service)
9. [API response & error code](#9-api-response--error-code)
10. [Definition of Done](#10-definition-of-done)
11. [Thứ tự & lộ trình thời gian](#11-thứ-tự--lộ-trình-thời-gian)

---

## 1. PHÂN BỔ NGÔN NGỮ & SERVICE

Mỗi ngôn ngữ được giao service theo đúng điểm mạnh, chia tương đối đều.

| Ngôn ngữ | Service phụ trách | Lý do chọn |
|---|---|---|
| **Go** | API Gateway, Cart, Inventory, Worker | Concurrency cao, latency thấp, ít logic nghiệp vụ nặng |
| **.NET** | Auth, User, Order, Payment | Transaction mạnh, EF Core, domain giao dịch phức tạp |
| **NestJS** | Product/Catalog, Notification | I/O-bound, realtime WebSocket, event-driven nhẹ |
| **Java Spring Boot** | Search, Promotion/Pricing | Domain phức tạp, hệ sinh thái lớn, hợp search + rule engine |

### Bảng service đầy đủ

| # | Service | Ngôn ngữ | Vai trò | Store |
|---|---|---|---|---|
| 1 | API Gateway | Go | Routing, auth edge, rate-limit, cache, correlation-id | Redis |
| 2 | Auth | .NET | Register, login, JWT, refresh token, role | auth_db |
| 3 | User | .NET | Profile, address, danh sách yêu thích | user_db |
| 4 | Product/Catalog | NestJS | Product, category, brand, variant, ảnh, giá hiển thị | product_db |
| 5 | Search | Java Spring Boot | Full-text search, filter nâng cao, gợi ý | Elasticsearch |
| 6 | Cart | Go | Giỏ hàng | Redis (+ cart_db optional) |
| 7 | Promotion/Pricing | Java Spring Boot | Coupon, khuyến mãi, tính giá cuối, thuế | promo_db |
| 8 | Order | .NET | Checkout, trạng thái đơn, saga participant | order_db |
| 9 | Inventory | Go | Tồn kho, reserve/release stock | inventory_db |
| 10 | Payment | .NET | Thanh toán mock, callback, refund | payment_db |
| 11 | Notification | NestJS | Thông báo + WebSocket realtime | notification_db |
| 12 | Media | Go hoặc NestJS | Upload & quản lý ảnh sản phẩm | MinIO/S3 |
| 13 | Worker | Go | Outbox publisher tập trung, cron, cleanup, saga timeout | (kết nối nhiều DB) |

> 13 service. Bạn không làm hết một lúc — xem [thứ tự ở mục 11](#11-thứ-tự--lộ-trình-thời-gian).

---

## 2. TECH STACK

> Số version chỉ tham khảo — luôn kiểm tra & lấy LTS/stable mới nhất khi bắt đầu.

| Thành phần | Tham khảo | Dùng cho |
|---|---|---|
| .NET / C# | .NET 10 LTS | Auth, User, Order, Payment |
| Node.js | LTS mới nhất (vd 24 LTS) | NestJS services |
| NestJS | 11.x | Product, Notification |
| TypeScript | 5.x mới nhất | NestJS services |
| Go | stable mới nhất | Gateway, Cart, Inventory, Media, Worker |
| Java | LTS mới nhất (21/25 LTS) | Search, Promotion |
| Spring Boot | 3.x mới nhất | Search, Promotion |
| PostgreSQL | bản mới nhất | DB per service |
| Elasticsearch/OpenSearch | bản mới nhất | Search service |
| Redis | bản mới nhất | Cart, cache, rate-limit, pub/sub |
| Kafka | bản mới nhất (KRaft) | Event bus chính |
| RabbitMQ | (tùy chọn thay thế) | Nếu muốn so sánh |
| MinIO / S3 | — | Lưu ảnh (Media) |
| Docker / Compose | mới nhất | Local dev |
| Kubernetes | kind/minikube/k3d | Deploy |
| k6 | mới nhất | Load test |
| OpenTelemetry | mới nhất | Tracing |
| Prometheus / Grafana / Loki / Tempo(Jaeger) | mới nhất | Observability |

**Kafka hay RabbitMQ?** Chọn Kafka làm chính (học được nhiều hơn: partition, consumer group, replay, DLQ topic). Có thể làm thêm nhánh RabbitMQ ở Phase nâng cao để so sánh.

---

## 3. SƠ ĐỒ KIẾN TRÚC ĐẦY ĐỦ

```
                              Client (Web / Mobile)
                                       │
                                       ▼
                          ┌────────────────────────┐
                          │    API Gateway (Go)     │  routing, auth edge,
                          │  rate-limit, cache, cid │  correlation-id
                          └────────────┬────────────┘
        ┌──────────┬──────────┬────────┼────────┬──────────┬───────────┐
        ▼          ▼          ▼        ▼        ▼          ▼           ▼
    ┌───────┐ ┌───────┐ ┌─────────┐ ┌─────┐ ┌───────┐ ┌────────┐ ┌─────────┐
    │ Auth  │ │ User  │ │ Product │ │Cart │ │Search │ │ Order  │ │Promotion│
    │ .NET  │ │ .NET  │ │ NestJS  │ │ Go  │ │ Java  │ │ .NET   │ │ Java    │
    └───┬───┘ └───┬───┘ └────┬────┘ └──┬──┘ └───┬───┘ └───┬────┘ └────┬────┘
        ▼         ▼          ▼         ▼        ▼         ▼           ▼
     auth_db   user_db   product_db  Redis   ES index  order_db    promo_db
                              │                            │
                              ▼ (đồng bộ index)            ▼
                          [Search consume ProductUpdated]  │
                                                           │
                    ┌──────────────────────────────────────┴───────────┐
                    │              Kafka  (Event Bus)                    │
                    │   Redis (cache / pub-sub / rate-limit)             │
                    └───┬───────────────┬───────────────┬───────────────┘
                        ▼               ▼               ▼
                  ┌───────────┐   ┌───────────┐   ┌──────────────┐
                  │ Inventory │   │ Payment   │   │ Notification │
                  │ Go        │   │ .NET      │   │ NestJS  (WS) │
                  └─────┬─────┘   └─────┬─────┘   └──────┬───────┘
                        ▼               ▼                ▼
                  inventory_db     payment_db      Redis Pub/Sub
                                                         │
                                                         ▼
                                                   WebSocket → Client

   ┌──────────┐   ┌──────────────────────────────────────────────┐
   │ Media    │   │ Worker (Go): outbox publisher tập trung,       │
   │ Go/Nest  │   │ cron cleanup, saga timeout, reservation expiry │
   │ → MinIO  │   └──────────────────────────────────────────────┘
   └──────────┘
```

---

## 4. CẤU TRÚC THƯ MỤC ĐÍCH

> Đích đến cuối cùng. KHÔNG tạo hết một lúc — mỗi phase thêm dần.

```
ecommerce-micro/
├── contracts/                       # Contract-first, dùng chung mọi service
│   ├── openapi/                     # OpenAPI spec từng service
│   ├── events/                      # Event envelope + schema từng event
│   ├── proto/                       # gRPC proto (nếu dùng)
│   └── errors/                      # Bảng error code chuẩn
│
├── services/
│   ├── gateway-go/                  # Go — API Gateway
│   │   ├── cmd/gateway/main.go
│   │   └── internal/{proxy,ratelimit,auth,cache,middleware}/
│   │
│   ├── auth-dotnet/                 # .NET — Auth
│   │   ├── Controllers/ Models/ Data/ Dtos/ Services/
│   │   ├── Program.cs appsettings.json Dockerfile
│   │
│   ├── user-dotnet/                 # .NET — User (profile, address)
│   │   └── (cấu trúc tương tự auth)
│   │
│   ├── product-nestjs/              # NestJS — Product/Catalog
│   │   └── src/{product,category,brand,variant,common,config}/
│   │
│   ├── search-java/                 # Java Spring Boot — Search
│   │   └── src/main/java/.../{controller,service,consumer,es}/
│   │
│   ├── cart-go/                     # Go — Cart (Redis)
│   │   └── cmd/api/main.go internal/{handler,repository,client}/
│   │
│   ├── promotion-java/              # Java Spring Boot — Promotion/Pricing
│   │   └── src/main/java/.../{controller,engine,rule,repository}/
│   │
│   ├── order-dotnet/                # .NET — Order (saga)
│   │   └── {Controllers,Domain,Data,Messaging,Sagas}/
│   │       # Domain: Order + outbox + inbox + idempotency
│   │
│   ├── inventory-go/                # Go — Inventory
│   │   └── internal/{handler,repository,consumer,producer}/
│   │
│   ├── payment-dotnet/              # .NET — Payment
│   │   └── {Controllers,Domain,Data,Messaging}/
│   │
│   ├── notification-nestjs/         # NestJS — Notification + WebSocket
│   │   └── src/{consumer,gateway,redis}/
│   │
│   ├── media-go/                    # Go — Media (MinIO/S3)
│   │   └── internal/{handler,storage}/
│   │
│   └── worker-go/                   # Go — Worker tập trung
│       └── internal/{outbox,scheduler,saga_timeout,cleanup}/
│
├── infra/
│   ├── docker-compose.yml           # chạy full local
│   ├── kafka/
│   ├── monitoring/                  # prometheus, grafana, loki, tempo
│   └── k8s/{deployments,services,ingress,hpa,configmaps,secrets}/
│
├── loadtest/                        # k6 scripts
│   └── {product,cart,checkout,websocket}.js
│
├── .github/workflows/               # CI/CD per stack
│
└── docs/
    └── architecture.md              # chính là file này
```

---

## 5. NGUYÊN TẮC VÀNG

1. **KHÔNG gộp service, KHÔNG cắt.** Mỗi service một trách nhiệm. Học từ từ nhưng làm đầy đủ.
2. **Database per Service tuyệt đối.** Service A không bao giờ chạm DB của service B. Cần dữ liệu → gọi API hoặc nghe event.
3. **Product KHÔNG giữ tồn kho thật.** Product chỉ giữ thông tin hiển thị. Tồn kho thật ở Inventory.
4. **Mỗi service stateless** (state đẩy ra Redis/DB) để scale ngang được.
5. **Mỗi phase phải có demo chạy + commit Git.** Theo [Definition of Done](#10-definition-of-done).
6. **Contract-first.** Định nghĩa event/API/error ở `contracts/` trước khi code.
7. **Observability thêm dần từ Phase 0**, không để cuối.
8. **Outbox + Inbox + Idempotency + DLQ** là bắt buộc cho mọi luồng event.

---

## 6. ROADMAP CHI TIẾT

---

### PHASE -1 — Project Foundation
**3–5 ngày · Mục tiêu:** dựng nền hạ tầng & quy chuẩn trước khi code service.

**Việc cần làm**
- [ ] Tạo monorepo + `README.md` tổng + `docs/architecture.md` (file này).
- [ ] Tạo `contracts/`: openapi, events, proto, errors.
- [ ] Chuẩn hóa: API response format, error code, **event envelope**, correlation-id.
- [ ] `docker-compose.yml` ban đầu: PostgreSQL, Redis, Kafka (KRaft).
- [ ] `.env.example`.
- [ ] Makefile/scripts: `up`, `down`, `logs`, `test`, `loadtest`.

**Tiêu chí hoàn thành:** `docker compose up -d` dựng được hạ tầng local.

---

### PHASE 0 — Product/Catalog Service (NestJS)
**1–2 tuần · DB:** product_db

**Mục tiêu:** làm tốt MỘT service trước khi phân tán.

**Việc cần làm**
- [ ] Khởi tạo NestJS, chọn ORM (Prisma hoặc TypeORM), kết nối Postgres.
- [ ] Bảng: `products`, `categories`, `brands`, `product_variants`, `product_images`.
- [ ] CRUD product / category / brand.
- [ ] Product list: paging, search cơ bản, filter theo category/brand, sort theo price/name/createdAt.
- [ ] Product detail, soft delete.
- [ ] `ValidationPipe` global + exception filter chuẩn hóa lỗi.
- [ ] **Giá lưu bằng integer (cents)**, không dùng float.
- [ ] Health check `/health` + `/ready`.
- [ ] Structured logging JSON.
- [ ] Dockerfile multi-stage + seed data.

**Lưu ý:** Product **không** có cột tồn kho thật. Chỉ: name, description, price, sku, brand, category, images, attributes, status.

**Tiêu chí hoàn thành:** CRUD + list/search/filter/sort chạy qua Docker, có seed, có health check, log JSON.

---

### PHASE 1 — Auth Service (.NET)
**2 tuần · DB:** auth_db

**Mục tiêu:** tách Auth riêng; Product tin JWT do Auth cấp.

**Việc cần làm**
- [ ] ASP.NET Core + EF Core + Npgsql.
- [ ] Bảng: `users`, `roles`, `user_roles`, `refresh_tokens`.
- [ ] Register, Login, Refresh token, Logout.
- [ ] Hash password bằng BCrypt hoặc Argon2.
- [ ] Access token (JWT) + refresh token (lưu dạng hash).
- [ ] Role: `CUSTOMER`, `ADMIN`.
- [ ] **Bảo mật:** rate-limit login, refresh token rotation, chống token reuse.
- [ ] Product service: thêm JWT guard cho route ghi; chỉ `ADMIN` được ghi product.
- [ ] Health check, structured logging, Dockerfile.

**JWT claim tối thiểu:** `sub`, `email`, `roles`, `jti`.

**Mấu chốt:** JWT_SECRET ở Auth phải GIỐNG secret Product dùng verify. Nâng cao → chuyển **RS256** (Auth giữ private key, service khác verify bằng public key).

**Tiêu chí hoàn thành:** register → login (access + refresh) → dùng token tạo product; không token/không phải ADMIN → 401/403; refresh hoạt động.

---

### PHASE 1.5 — API Gateway bản mỏng (Go)
**3–7 ngày**

**Mục tiêu:** có một cửa vào duy nhất sớm.

**Việc cần làm**
- [ ] Reverse proxy: `/auth/**` → Auth, `/products/**` → Product.
- [ ] Correlation-id: nhận `X-Correlation-Id` hoặc tự sinh.
- [ ] Request logging.
- [ ] Verify JWT tại Gateway cho route private.
- [ ] Forward user context: `X-User-Id`, `X-User-Email`, `X-User-Roles`.
- [ ] Timeout mặc định + health check.

**Chưa làm ở đây:** rate-limit, cache, circuit breaker (để Phase 9).

**Tiêu chí hoàn thành:** client chỉ gọi Gateway, không gọi trực tiếp service.

---

### PHASE 2 — User Service (.NET)
**1 tuần · DB:** user_db

**Mục tiêu:** tách hồ sơ người dùng khỏi Auth (Auth chỉ lo xác thực, User lo hồ sơ).

**Việc cần làm**
- [ ] Bảng: `user_profiles`, `addresses`, `wishlists`.
- [ ] API: xem/sửa profile, CRUD địa chỉ, wishlist.
- [ ] Nghe event `UserRegistered` (từ Auth) → tạo profile rỗng tương ứng.
- [ ] Health check, logging, Dockerfile, Gateway route `/users/**`.

**Tiêu chí hoàn thành:** đăng ký ở Auth → User tự tạo profile qua event; sửa profile/địa chỉ chạy được.

> Đây là lần đầu hai service đồng bộ trạng thái qua **event**, không gọi trực tiếp.

---

### PHASE 3 — Cart Service (Go) + sync call đầu tiên
**1 tuần · Store:** Redis (+ cart_db optional)

**Mục tiêu:** học Redis thực tế + xử lý gọi đồng bộ service-to-service.

**Việc cần làm**
- [ ] API: `GET /cart`, `POST /cart/items`, `PUT /cart/items/{variantId}`, `DELETE /cart/items/{variantId}`, `DELETE /cart`.
- [ ] Cart lưu Redis, key `cart:{userId}`, TTL 30 ngày.
- [ ] Khi add item: **gọi Product service** lấy snapshot (tên, variant, giá) lưu vào cart.
- [ ] **Xử lý lỗi sync call:** timeout + fallback khi Product chết (đây là điểm học chính).
- [ ] Validate quantity > 0.
- [ ] Health check, Dockerfile, Gateway route `/cart/**`.

**Tiêu chí hoàn thành:** add sản phẩm vào cart; cart nằm Redis; restart service không mất cart; Product chết thì Cart báo lỗi đẹp, không treo.

---

### PHASE 4 — Search Service (Java Spring Boot) + Elasticsearch
**1–2 tuần · Store:** Elasticsearch

**Mục tiêu:** full-text search thật, khác hẳn filter SQL. Đây là nơi **Java Spring Boot** vào hệ thống.

**Việc cần làm**
- [ ] Spring Boot + Spring Data Elasticsearch.
- [ ] Index sản phẩm: tên, mô tả, brand, category, attributes, price.
- [ ] API: full-text search, filter nhiều chiều, sort, paging, gợi ý (autocomplete).
- [ ] **Đồng bộ index qua event:** consume `ProductCreated`/`ProductUpdated`/`ProductDeleted` từ Product → cập nhật ES.
- [ ] Xử lý reindex toàn bộ (cron hoặc API admin).
- [ ] Health check, structured logging, Dockerfile, Gateway route `/search/**`.

**Tiêu chí hoàn thành:** search trả kết quả full-text + filter; sửa product ở Product service → vài giây sau search phản ánh đúng (eventual consistency qua event).

> Đây cũng là service đầu tiên **xây read-model riêng từ event** — một pattern quan trọng (CQRS nhẹ).

---

### PHASE 5 — Order + Inventory + Message Broker
**2–3 tuần · DB:** order_db, inventory_db · **Broker:** Kafka

**Mục tiêu:** event-driven thật sự — trái tim của microservices.

**Order (.NET)**
- [ ] Bảng: `orders`, `order_items`, `order_status_histories`, `outbox_messages`, `inbox_messages`, `idempotency_keys`.
- [ ] API: `POST /orders/checkout`, `GET /orders`, `GET /orders/{id}`, `POST /orders/{id}/cancel`.
- [ ] Checkout: lấy cart snapshot → tạo order `PENDING` → lưu order item snapshot → ghi outbox `OrderCreated` (cùng transaction).
- [ ] **Outbox publisher** (ban đầu chạy ngay trong Order như background job; tách sang Worker ở Phase 12): đọc outbox → publish → mark processed → retry nếu lỗi.

**Inventory (Go)**
- [ ] Bảng: `inventory_items`, `stock_reservations`, `stock_movements`, `inbox_messages`, `outbox_messages`.
- [ ] Consume `OrderCreated` → reserve stock bằng **SQL atomic update** (chống oversell):
  ```sql
  UPDATE inventory_items
  SET available_quantity = available_quantity - @qty,
      reserved_quantity  = reserved_quantity  + @qty,
      updated_at = now()
  WHERE variant_id = @variantId AND available_quantity >= @qty;
  ```
  affected rows = 0 → thiếu hàng.
- [ ] Publish `StockReserved` / `StockFailed`.
- [ ] **Idempotent consumer** dùng inbox table (Kafka at-least-once → message tới 2 lần).

**Tiêu chí hoàn thành:** checkout tạo order PENDING; Inventory tự reserve qua event; đủ hàng → StockReserved, thiếu → StockFailed; không service nào query DB service khác.

---

### PHASE 6 — Promotion/Pricing Service (Java Spring Boot)
**1–2 tuần · DB:** promo_db

**Mục tiêu:** engine tính giá cuối — chỗ thứ hai cho **Java**. Domain rule phức tạp, hợp Spring.

**Việc cần làm**
- [ ] Bảng: `promotions`, `coupons`, `promotion_rules`, `coupon_redemptions`.
- [ ] API tính giá: nhận giỏ hàng → áp dụng rule (giảm %, giảm tiền, mua X tặng Y, freeship) → trả breakdown giá cuối (subtotal, discount, tax, shipping, total).
- [ ] Validate coupon: hết hạn, hết lượt, điều kiện tối thiểu.
- [ ] **Tích hợp checkout:** Order gọi Promotion lúc checkout để chốt giá; lưu snapshot giá vào order.
- [ ] Chống lạm dụng coupon (idempotent redemption).
- [ ] Health check, logging, Dockerfile, Gateway route `/promotions/**`.

**Tiêu chí hoàn thành:** áp coupon hợp lệ → giá giảm đúng; coupon sai/hết hạn → từ chối; checkout phản ánh đúng giá đã giảm.

---

### PHASE 7 — Saga + Payment Service (.NET)
**2–3 tuần · DB:** payment_db

**Mục tiêu:** hoàn chỉnh luồng checkout + xử lý giao dịch phân tán.

**Payment (.NET)**
- [ ] Bảng: `payments`, `payment_logs`, `idempotency_keys`, `inbox_messages`, `outbox_messages`.
- [ ] Consume `StockReserved` → tạo payment PENDING.
- [ ] Mock success/fail + API callback giả lập `POST /payments/mock-callback`.
- [ ] Publish `PaymentCompleted` / `PaymentFailed`.

**Order bổ sung**
- [ ] Consume `StockReserved`, `StockFailed`, `PaymentCompleted`, `PaymentFailed`.
- [ ] State machine: `PENDING → INVENTORY_RESERVED → PAYMENT_PENDING → PAID → CONFIRMED` / `CANCELLED` / `FAILED`.
- [ ] Lưu `order_status_histories`.

**Inventory bổ sung**
- [ ] Consume `PaymentFailed` → release stock → publish `StockReleased`.

**⭐ BỔ KHUYẾT QUAN TRỌNG (đừng bỏ):**
- [ ] **Saga timeout / orphan handling:** mỗi bước saga có deadline. Nếu Payment không bao giờ callback → order treo. Cần cron (ở Worker, Phase 12) quét order quá hạn → cancel + release stock.
- [ ] **Reservation expiry:** stock đã reserve mà không thanh toán trong X phút → tự release. Không có cái này thì kho bị "giam" vĩnh viễn.

**Saga flows**
```
Thành công:  OrderCreated → StockReserved → PaymentCompleted → OrderConfirmed → NotificationCreated
Hết hàng:    OrderCreated → StockFailed → OrderCancelled → NotificationCreated
Payment lỗi: OrderCreated → StockReserved → PaymentFailed → StockReleased → OrderCancelled → NotificationCreated
Timeout:     OrderCreated → StockReserved → (payment im lặng) → [timeout] → StockReleased → OrderCancelled
```

**Tiêu chí hoàn thành:** payment success → order CONFIRMED; fail → CANCELLED + stock released; callback gửi trùng không làm sai trạng thái (idempotency); payment im lặng → saga timeout tự hủy đơn.

> Phần khó nhất, phân biệt middle với junior. Dành thời gian.

---

### PHASE 8 — Realtime WebSocket (NestJS)
**1–2 tuần · Store:** Redis Pub/Sub

**Mục tiêu:** đẩy trạng thái đơn hàng realtime; scale qua nhiều instance.

**Việc cần làm**
- [ ] Notification service: WebSocket Gateway; client connect bằng JWT; map connection theo `userId`.
- [ ] Subscribe channel `user:{userId}`, `order:{orderId}`.
- [ ] Consumer nhận `OrderConfirmed`/`OrderCancelled` → publish vào **Redis Pub/Sub** → instance đang giữ connection emit cho client (giải bài toán WebSocket stateful khi scale).
- [ ] Lưu notification vào `notification_db` (lịch sử).
- [ ] Chạy 2+ instance Notification để test scale.
- [ ] Gateway route WebSocket + k6 WebSocket test cơ bản.

**Tiêu chí hoàn thành:** checkout xong nhận status realtime; 2 instance vẫn nhận đúng; disconnect/reconnect không lỗi.

---

### PHASE 9 — Media Service + Gateway nâng cao + Cache + Resilience
**2 tuần · Ngôn ngữ:** Media (Go), Gateway (Go)

**Mục tiêu:** ảnh sản phẩm + biến Gateway thành lớp bảo vệ.

**Media (Go)**
- [ ] Upload ảnh → lưu MinIO/S3; trả URL.
- [ ] Resize/thumbnail (tùy chọn); validate file type/size.
- [ ] Product tham chiếu URL ảnh từ Media.

**Gateway nâng cao**
- [ ] Rate-limit bằng Redis (theo IP + theo userId).
- [ ] Cache-aside cho product list/detail; **invalidate khi `ProductUpdated`**.
- [ ] Timeout per route; retry chỉ cho GET; **circuit breaker**.
- [ ] Body size limit, CORS, compression (tùy chọn).
- [ ] Metrics cơ bản: request count, latency, status code.

**Tiêu chí hoàn thành:** upload ảnh & gắn vào product; vượt rate-limit → 429; product detail lần 2 nhanh hơn nhờ cache; product update xóa cache đúng; service phía sau chết → Gateway fail fast.

---

### PHASE 10 — Observability chuẩn
**1–2 tuần (nhưng làm dần từ Phase 0)**

**Mục tiêu:** debug được hệ phân tán.

**Việc cần làm**
- [ ] OpenTelemetry SDK cho cả 4 ngôn ngữ (.NET, NestJS, Go, Java).
- [ ] Trace HTTP request, DB query, broker publish/consume.
- [ ] **Propagate trace context qua Kafka headers** (xuyên service).
- [ ] Prometheus metrics + Grafana dashboard cho mọi service + Postgres + Redis + Kafka.
- [ ] Loki (logs) + Tempo/Jaeger (traces).

**Log field bắt buộc:** `timestamp, level, service, environment, traceId, spanId, correlationId, requestId, userId, message, durationMs, error`.

**Tiêu chí hoàn thành:** một order lỗi → trace được toàn flow: `Gateway → Order → Kafka → Inventory → Kafka → Payment → Kafka → Order → Notification → WebSocket`.

---

### PHASE 11 — Load Test & Performance
**2–3 tuần · Tool:** k6

**Mục tiêu:** đo thật, không đoán. Hiện thực hóa mục tiêu 10k.

**SLO mục tiêu (định nghĩa rõ con số):**
```
10.000 concurrent users
Product list   p95 < 300ms
Product detail p95 < 200ms
Checkout       p95 < 1000ms
WebSocket      10.000 concurrent connections
Error rate     < 1%
```

**Việc cần làm**
- [ ] k6 cho: product list, product detail, cart flow, checkout flow, WebSocket.
- [ ] Baseline 100 → 1.000 → 5.000 → 10.000 users; ghi report mỗi lần.
- [ ] Tìm bottleneck: CPU, RAM, DB connection, slow query, Redis latency, broker lag, GC, network.
- [ ] Tối ưu: index, cache, connection pool, **PgBouncer**, consumer concurrency, batch outbox, **read replica**, horizontal scale.

**Tiêu chí hoàn thành:** trả lời được — bottleneck ở đâu, đã tối ưu gì, trước/sau khác nhau bao nhiêu, p95/p99 & error rate là bao nhiêu.

---

### PHASE 12 — Worker Service (Go) tách riêng
**1 tuần · Ngôn ngữ:** Go

**Mục tiêu:** gom các tác vụ nền vào service chuyên trách (trước đó chạy nhúng trong từng service).

**Việc cần làm**
- [ ] Outbox publisher tập trung (đọc outbox của các service → publish).
- [ ] Scheduler/cron: dọn idempotency key cũ, dọn cart hết hạn.
- [ ] **Saga timeout job:** quét order treo quá deadline → trigger compensation.
- [ ] **Reservation expiry job:** quét stock reservation quá hạn → release.

**Tiêu chí hoàn thành:** các job nền chạy ổn định, độc lập; order treo & stock giam được tự xử lý.

---

### PHASE 13 — Kubernetes, CI/CD, Broker nâng cao
**3–5 tuần**

**Kubernetes**
- [ ] Hoàn thiện Dockerfile mọi service; docker-compose full local.
- [ ] Manifest: Deployment, Service, ConfigMap, Secret, Ingress, **HPA**, PVC nếu cần.
- [ ] Liveness/readiness probe; resource requests/limits; namespace.
- [ ] Helm hoặc Kustomize; deploy lên kind/minikube/k3d; test HPA bằng load.

**CI/CD (GitHub Actions)**
- [ ] .NET: restore/build/test/format. NestJS: install/lint/test/build. Go: test/vet/gofmt. Java: build/test (Maven/Gradle).
- [ ] Build Docker image; security scan (tùy chọn); migration check; PR fail CI → không merge.

**Broker nâng cao**
- [ ] Kafka: nhiều partition, consumer group scaling, **retry topic + DLQ**, event replay, Schema Registry (tùy chọn).
- [ ] (Tùy chọn) Làm nhánh RabbitMQ: topic exchange, quorum queue, DLX, prefetch tuning — để so sánh với Kafka.

**Tiêu chí hoàn thành:** `kubectl apply` chạy toàn hệ thống; Ingress expose Gateway; tăng tải → pod tự scale; PR phải qua CI mới merge; message lỗi vào DLQ thay vì chặn partition.

---

## 7. EVENT CONTRACT CHUẨN

**Event envelope** (mọi event tuân theo):
```json
{
  "eventId": "uuid",
  "eventName": "OrderCreated",
  "eventVersion": 1,
  "occurredAt": "2026-05-31T12:00:00Z",
  "source": "order-service",
  "correlationId": "uuid",
  "causationId": "uuid",
  "data": {}
}
```

**Danh sách event tối thiểu:**
```
UserRegistered · ProductCreated · ProductUpdated · ProductDeleted
CartCheckedOut · PriceCalculated · CouponRedeemed
OrderCreated · StockReserved · StockFailed · StockReleased
PaymentPending · PaymentCompleted · PaymentFailed
OrderConfirmed · OrderCancelled · NotificationCreated
```

---

## 8. DATABASE PER SERVICE

```
auth_db:       users, roles, user_roles, refresh_tokens
user_db:       user_profiles, addresses, wishlists
product_db:    products, categories, brands, product_variants, product_images
search:        (Elasticsearch index, không phải SQL)
cart:          Redis (cart_db optional: cart_snapshots)
promo_db:      promotions, coupons, promotion_rules, coupon_redemptions
order_db:      orders, order_items, order_status_histories, idempotency_keys, outbox_messages, inbox_messages
inventory_db:  inventory_items, stock_reservations, stock_movements, inbox_messages, outbox_messages
payment_db:    payments, payment_logs, idempotency_keys, inbox_messages, outbox_messages
notification_db: notifications, notification_deliveries
media:         MinIO/S3 (metadata optional trong media_db)
```

---

## 9. API RESPONSE & ERROR CODE

**Success**
```json
{ "success": true, "data": {}, "meta": { "requestId": "uuid", "correlationId": "uuid" } }
```
**Error**
```json
{ "success": false,
  "error": { "code": "ORDER_NOT_FOUND", "message": "Order not found", "details": {} },
  "meta": { "requestId": "uuid", "correlationId": "uuid" } }
```

**Error code tối thiểu**
```
AUTH_INVALID_CREDENTIALS · AUTH_TOKEN_EXPIRED · AUTH_REFRESH_TOKEN_INVALID · AUTH_FORBIDDEN
PRODUCT_NOT_FOUND · PRODUCT_VARIANT_NOT_FOUND · PRODUCT_INACTIVE
SEARCH_QUERY_INVALID
CART_EMPTY · CART_ITEM_NOT_FOUND · CART_INVALID_QUANTITY
PROMO_COUPON_INVALID · PROMO_COUPON_EXPIRED · PROMO_COUPON_EXHAUSTED
ORDER_NOT_FOUND · ORDER_INVALID_STATUS · ORDER_ALREADY_CANCELLED · ORDER_DUPLICATED_REQUEST
INVENTORY_NOT_ENOUGH_STOCK · INVENTORY_RESERVATION_EXPIRED
PAYMENT_FAILED · PAYMENT_TIMEOUT · PAYMENT_ALREADY_PROCESSED
BROKER_PUBLISH_FAILED · BROKER_CONSUME_FAILED
SYSTEM_INTERNAL_ERROR · SYSTEM_TIMEOUT · SYSTEM_SERVICE_UNAVAILABLE
```

---

## 10. DEFINITION OF DONE

Một phase chỉ "xong" khi:
- [ ] Chạy được local bằng Docker/script.
- [ ] Có README riêng cho service/phase.
- [ ] Có health check + logging có cấu trúc.
- [ ] Có migration/schema rõ ràng + seed nếu cần.
- [ ] Có test cơ bản.
- [ ] Có API/event contract trong `contracts/`.
- [ ] Không hardcode secret.
- [ ] Có API versioning (`/v1/`).
- [ ] Commit Git riêng.

---

## 11. THỨ TỰ & LỘ TRÌNH THỜI GIAN

```
Phase -1  Foundation
Phase 0   Product            (NestJS)
Phase 1   Auth               (.NET)
Phase 1.5 Gateway mỏng       (Go)
Phase 2   User               (.NET)
Phase 3   Cart               (Go)        — sync call + Redis
Phase 4   Search             (Java)      — ES + read-model từ event
Phase 5   Order + Inventory  (.NET + Go) — Kafka, Outbox/Inbox
Phase 6   Promotion/Pricing  (Java)
Phase 7   Saga + Payment     (.NET)      — + saga timeout + reservation expiry
Phase 8   WebSocket          (NestJS)
Phase 9   Media + Gateway nâng cao (Go)
Phase 10  Observability
Phase 11  Load Test
Phase 12  Worker tách riêng  (Go)
Phase 13  Kubernetes + CI/CD + Broker nâng cao
```

**Core tối thiểu để có một ecommerce đúng bản chất:** `-1 → 0 → 1 → 1.5 → 3 → 5 → 7`. Làm xong là đã chạy được luồng mua hàng đầu-cuối. Các phase còn lại bồi đắp cho đầy đủ.

| Giai đoạn | Phase | Thời lượng |
|---|---:|---:|
| Nền tảng | -1 | 3–5 ngày |
| Product | 0 | 1–2 tuần |
| Auth | 1 | 2 tuần |
| Gateway mỏng | 1.5 | 3–7 ngày |
| User | 2 | 1 tuần |
| Cart | 3 | 1 tuần |
| Search | 4 | 1–2 tuần |
| Order + Inventory + Broker | 5 | 2–3 tuần |
| Promotion/Pricing | 6 | 1–2 tuần |
| Saga + Payment | 7 | 2–3 tuần |
| WebSocket | 8 | 1–2 tuần |
| Media + Gateway nâng cao | 9 | 2 tuần |
| Observability | 10 | 1–2 tuần |
| Load test | 11 | 2–3 tuần |
| Worker | 12 | 1 tuần |
| K8s + CI/CD + Broker nâng cao | 13 | 3–5 tuần |

**Tổng ước lượng (bán thời gian):** khoảng 6–9 tháng cho toàn bộ 13 service. Đây là dự án lớn — đi từ từ, mỗi phase một mục tiêu, commit đều.

---

## GHI CHÚ CUỐI

- **4 ngôn ngữ phân đều:** Go (Gateway, Cart, Inventory, Media, Worker), .NET (Auth, User, Order, Payment), NestJS (Product, Notification), Java (Search, Promotion).
- **Java vào ở Phase 4 & 6** — hai chỗ domain phức tạp nhất (search engine, pricing/rule engine), nơi Spring tỏa sáng.
- Hai bổ khuyết logic quan trọng nhất nằm ở **Phase 7**: saga timeout và reservation expiry — đừng bỏ, đó là thứ làm saga "thật".
- Mỗi pattern (Outbox, Inbox, Saga, CQRS read-model, cache invalidation) sau khi làm xong nên **tự viết lại bằng lời của bạn** để học sâu.
