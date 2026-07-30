# E-commerce Microservices

> Toàn bộ hệ thống dùng một ngôn ngữ duy nhất: C# / .NET 10 (LTS).
> Mục tiêu vẫn là học microservices đúng bản chất — nhưng tập trung vào kiến trúc, pattern và hệ sinh thái .NET.

---

## MỤC LỤC

1. [Danh sách service](#1-danh-sách-service)
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

## 1. DANH SÁCH SERVICE

Tất cả service viết bằng **.NET 10**. Mỗi service một trách nhiệm rõ ràng, một database riêng.

| # | Service | Project type | Vai trò | Store |
|---|---|---|---|---|
| 1 | API Gateway | ASP.NET Core + **YARP** | Routing, auth edge, rate-limit, cache, correlation-id | Redis |
| 2 | Auth | ASP.NET Core Web API | Register, login, JWT, refresh token, role | auth_db |
| 3 | User | ASP.NET Core Web API | Profile, address, danh sách yêu thích | user_db |
| 4 | Product/Catalog | ASP.NET Core Web API | Product, category, brand, variant, ảnh, giá hiển thị | product_db |
| 5 | Search | ASP.NET Core Web API | Full-text search, filter nâng cao, gợi ý | Elasticsearch |
| 6 | Cart | ASP.NET Core Web API | Giỏ hàng | Redis (+ cart_db optional) |
| 7 | Promotion/Pricing | ASP.NET Core Web API | Coupon, khuyến mãi, tính giá cuối, thuế | promo_db |
| 8 | Order | ASP.NET Core Web API | Checkout, trạng thái đơn, saga participant | order_db |
| 9 | Inventory | ASP.NET Core Web API | Tồn kho, reserve/release stock | inventory_db |
| 10 | Payment | ASP.NET Core Web API | Thanh toán mock, callback, refund | payment_db |
| 11 | Notification | ASP.NET Core + **SignalR** | Thông báo + WebSocket realtime | notification_db |
| 12 | Media | ASP.NET Core Web API | Upload & quản lý ảnh sản phẩm | MinIO/S3 |
| 13 | Worker | **.NET Worker Service** | Outbox publisher tập trung, cron, cleanup, saga timeout | (kết nối nhiều DB) |

> 13 service. Không làm hết một lúc — xem [thứ tự ở mục 11](#11-thứ-tự--lộ-trình-thời-gian).

**Vì sao chỉ dùng .NET?** Một stack duy nhất giúp: tái sử dụng tối đa thư viện chung (logging, contracts, messaging, outbox/inbox), một bộ CI/CD đồng nhất, dễ refactor xuyên service, và đi sâu vào hệ sinh thái .NET (EF Core, YARP, SignalR, MassTransit/Confluent.Kafka, OpenTelemetry) thay vì học hời hợt 4 ngôn ngữ.

---

## 2. TECH STACK

> Số version chỉ tham khảo — luôn kiểm tra & lấy LTS/stable mới nhất khi bắt đầu.

| Thành phần | Tham khảo | Dùng cho |
|---|---|---|
| .NET / C# | **.NET 10 LTS / C# 14** | Mọi service |
| ASP.NET Core | 10.x | Web API mọi service |
| EF Core | 10.x | ORM cho các service dùng SQL |
| Npgsql | mới nhất | Provider PostgreSQL cho EF Core |
| YARP | mới nhất | Reverse proxy cho API Gateway |
| SignalR | (kèm ASP.NET Core) | WebSocket realtime (Notification) |
| MassTransit **hoặc** Confluent.Kafka | mới nhất | Messaging / consumer-producer Kafka |
| StackExchange.Redis | mới nhất | Cart, cache, rate-limit, pub/sub backplane |
| Elastic.Clients.Elasticsearch | mới nhất | Search service |
| AWSSDK.S3 / Minio | mới nhất | Lưu ảnh (Media) |
| Serilog | mới nhất | Structured logging JSON |
| OpenTelemetry .NET | mới nhất | Tracing/metrics/logs |
| FluentValidation | mới nhất | Validate request/DTO |
| MediatR | mới nhất | CQRS (command/query) + domain event nội bộ (Order, Payment) |
| Grpc.AspNetCore | mới nhất | Sync call nội bộ tần suất cao (thay REST nội bộ) |
| OpenIddict | mới nhất | OIDC/OAuth2 provider cho Auth |
| Microsoft.Extensions.Http.Resilience / Polly | mới nhất | Resilience: retry, timeout, circuit breaker |
| .NET Aspire | 13.x | Orchestrate local, ServiceDefaults, dashboard |
| xUnit + Testcontainers | mới nhất | Unit/integration test |
| PostgreSQL | bản mới nhất | DB per service |
| Elasticsearch/OpenSearch | bản mới nhất | Search service |
| Redis | bản mới nhất | Cart, cache, rate-limit, pub/sub |
| Kafka | bản mới nhất (KRaft) | Event bus chính |
| RabbitMQ | (tùy chọn thay thế) | Nếu muốn so sánh |
| MinIO / S3 | — | Lưu ảnh (Media) |
| Docker / Compose | mới nhất | Local dev |
| Kubernetes | kind/minikube/k3d | Deploy |
| k6 | mới nhất | Load test |
| Prometheus / Grafana / Loki / Tempo(Jaeger) | mới nhất | Observability |

**Kafka hay RabbitMQ?** Chọn Kafka làm chính (học được nhiều hơn: partition, consumer group, replay, DLQ topic). Có thể làm thêm nhánh RabbitMQ ở Phase nâng cao để so sánh. Với .NET, **MassTransit** cho trải nghiệm cao cấp (consumer, retry, saga state machine sẵn có), còn **Confluent.Kafka** cho mức kiểm soát thấp hơn — nên thử cả hai để hiểu trade-off.

**Dùng chung:** dùng **.NET Aspire** làm orchestrator local chính (service discovery, dashboard, wiring Redis/Postgres/Kafka) song song với docker-compose; gói cross-cutting (health check, OpenTelemetry, resilience, service discovery) vào một project **`ServiceDefaults`** để mọi service bật bằng một dòng — đúng cách eShop làm.

**Sync call nội bộ:** call public qua Gateway dùng REST; call nội bộ tần suất cao (vd Cart → Product) ưu tiên **gRPC** (latency thấp, hợp đồng kiểu mạnh) — eShop dùng gRPC cho Basket → Catalog. REST nội bộ vẫn ổn cho luồng đơn giản.

**DDD + CQRS:** service giao dịch (Order, Payment) viết theo **DDD chiến thuật** (aggregate, value object, domain event) + **CQRS bằng MediatR** (tách command/query) — giống eShop Ordering. Service CRUD thuần (Product, User, Cart...) giữ đơn giản, không ép DDD nặng.

---

## 3. SƠ ĐỒ KIẾN TRÚC ĐẦY ĐỦ

```
                              Client (Web / Mobile)
                                       │
                                       ▼
                          ┌─────────────────────────┐
                          │  API Gateway (YARP)     │  routing, auth edge,
                          │  rate-limit, cache, cid │  correlation-id
                          └────────────┬────────────┘
        ┌──────────┬──────────┬────────┼────────┬──────────┬───────────┐
        ▼          ▼          ▼        ▼        ▼          ▼           ▼
    ┌───────┐ ┌───────┐ ┌─────────┐ ┌─────┐ ┌───────┐ ┌────────┐ ┌─────────┐
    │ Auth  │ │ User  │ │ Product │ │Cart │ │Search │ │ Order  │ │Promotion│
    │ .NET  │ │ .NET  │ │ .NET    │ │.NET │ │ .NET  │ │ .NET   │ │ .NET    │
    └───┬───┘ └───┬───┘ └────┬────┘ └──┬──┘ └───┬───┘ └───┬────┘ └────┬────┘
        ▼         ▼          ▼         ▼        ▼         ▼           ▼
     auth_db   user_db   product_db  Redis   ES index  order_db    promo_db
                              │                            │
                              ▼ (đồng bộ index)            ▼
                          [Search consume ProductUpdated]  │
                                                           │
                    ┌──────────────────────────────────────┴───────────┐
                    │              Kafka  (Event Bus)                  │
                    │   Redis (cache / pub-sub / rate-limit)           │
                    └───┬───────────────┬───────────────┬──────────────┘
                        ▼               ▼               ▼
                  ┌───────────┐   ┌───────────┐   ┌───────────────┐
                  │ Inventory │   │ Payment   │   │ Notification  │
                  │ .NET      │   │ .NET      │   │ .NET (SignalR)│
                  └─────┬─────┘   └─────┬─────┘   └──────┬────────┘
                        ▼               ▼                ▼
                  inventory_db     payment_db      Redis backplane
                                                         │
                                                         ▼
                                                   WebSocket → Client

   ┌──────────┐   ┌──────────────────────────────────────────────┐
   │ Media    │   │ Worker (.NET Worker Service):                │
   │ .NET     │   │ outbox publisher tập trung, cron cleanup,    │
   │ → MinIO  │   │ saga timeout, reservation expiry             │
   └──────────┘   └──────────────────────────────────────────────┘
```

---

## 4. CẤU TRÚC THƯ MỤC ĐÍCH

> Đích đến cuối cùng. Không tạo hết một lúc — mỗi phase thêm dần.

```
ecommerce-micro/
├── Ecommerce.sln                    # Solution chung cho toàn bộ service
│
├── Ecommerce.AppHost/               # .NET Aspire orchestrator (wiring Postgres/Redis/Kafka + service)
│
├── contracts/                       # Contract-first, dùng chung mọi service
│   ├── openapi/                     # OpenAPI spec từng service
│   ├── proto/                       # gRPC proto cho sync call nội bộ
│   ├── events/                      # Event envelope + schema từng event
│   └── errors/                      # Bảng error code chuẩn
│
├── shared/                          # Class library dùng chung (NuGet nội bộ hoặc project ref)
│   ├── Ecommerce.ServiceDefaults/   # Health check + OpenTelemetry + service discovery + resilience handler (kiểu eShop, dùng với Aspire)
│   ├── Ecommerce.BuildingBlocks/    # ApiResponse, error codes, correlation-id, result type
│   ├── Ecommerce.BuildingBlocks.Ddd/ # AggregateRoot, Entity, ValueObject, DomainEvent base (cho Order/Payment)
│   ├── Ecommerce.Messaging/         # Event envelope, Kafka producer/consumer abstraction
│   └── Ecommerce.Outbox/            # Outbox/Inbox/Idempotency pattern tái dùng
│
├── services/
│   ├── Gateway/                     # ASP.NET Core + YARP — API Gateway
│   │   ├── Program.cs appsettings.json Dockerfile
│   │   └── (proxy config, ratelimit, auth, cache, middleware)
│   │
│   ├── Auth/                        # Auth
│   │   └── {Controllers,Models,Data,Dtos,Services}/  Program.cs Dockerfile
│   │
│   ├── User/                        # User (profile, address)
│   │   └── (cấu trúc tương tự Auth)
│   │
│   ├── Product/                     # Product/Catalog
│   │   └── {Controllers,Domain,Data,Dtos,Services}/
│   │
│   ├── Search/                      # Search (Elasticsearch)
│   │   └── {Controllers,Services,Consumers,Indexing}/
│   │
│   ├── Cart/                        # Cart (Redis)
│   │   └── {Controllers,Repositories,Clients}/
│   │
│   ├── Promotion/                   # Promotion/Pricing
│   │   └── {Controllers,Engine,Rules,Data}/
│   │
│   ├── Order/                       # Order (DDD + CQRS + saga)
│   │   └── {Controllers,Application,Domain,Infrastructure,Messaging,Sagas}/
│   │       # Application: command/query handler (MediatR)
│   │       # Domain: Order aggregate + value object + domain event
│   │       # Infrastructure: EF Core + outbox + inbox + idempotency
│   │
│   ├── Inventory/                   # Inventory
│   │   └── {Controllers,Domain,Data,Consumers,Producers}/
│   │
│   ├── Payment/                     # Payment
│   │   └── {Controllers,Domain,Data,Messaging}/
│   │
│   ├── Notification/                # Notification + SignalR
│   │   └── {Hubs,Consumers,Data,Redis}/
│   │
│   ├── Media/                       # Media (MinIO/S3)
│   │   └── {Controllers,Storage}/
│   │
│   └── Worker/                      # .NET Worker Service tập trung
│       └── {Outbox,Scheduler,SagaTimeout,Cleanup}/  (BackgroundService)
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
├── .github/workflows/               # CI/CD (.NET build/test/publish)
│
└── docs/
    └── architecture.md              # chính là file này
```

---

## 5. NGUYÊN TẮC VÀNG

1. **Không gộp service, không cắt.** Mỗi service một trách nhiệm. Học từ từ nhưng làm đầy đủ.
2. **Database per Service tuyệt đối.** Service A không bao giờ chạm DB của service B. Cần dữ liệu → gọi API hoặc nghe event.
3. **Product không giữ tồn kho thật.** Product chỉ giữ thông tin hiển thị. Tồn kho thật ở Inventory.
4. **Mỗi service stateless** (state đẩy ra Redis/DB) để scale ngang được.
5. **Mỗi phase phải có demo chạy + commit Git.** Theo [Definition of Done](#10-definition-of-done).
6. **Contract-first.** Định nghĩa event/API/error ở `contracts/` trước khi code.
7. **Observability thêm dần từ Phase 0**, không để cuối.
8. **Outbox + Inbox + Idempotency + DLQ** là bắt buộc cho mọi luồng event.
9. **Một stack — tái dùng tối đa.** Logic chung (response format, outbox, messaging, telemetry) nằm ở `shared/`; tuyệt đối **không copy-paste giữa service**. Nhưng vẫn giữ ranh giới: shared chỉ chứa building block kỹ thuật, không chứa domain của service nào.
10. **DDD + CQRS cho service giao dịch.** Order và Payment dùng aggregate, value object, domain event, tách command/query bằng MediatR. Service CRUD thuần (Product, User, Cart, Media...) giữ đơn giản, không ép DDD nặng.
11. **ServiceDefaults dùng một dòng.** Health check, telemetry, resilience, service discovery gói trong `Ecommerce.ServiceDefaults`; mọi service gọi `builder.AddServiceDefaults()` để bật, không tự cấu hình lại từng nơi.

---

## 6. ROADMAP CHI TIẾT

---

### PHASE -1 — Project Foundation
**3–5 ngày · Mục tiêu:** dựng nền hạ tầng & quy chuẩn trước khi code service.

**Việc cần làm**
- [x] Tạo monorepo + `Ecommerce.sln` + `README.md` tổng + `docs/architecture.md` (file này).
- [x] Tạo `contracts/`: openapi, events, errors.
- [x] Tạo `shared/` building blocks: `ApiResponse<T>`, error code, correlation-id middleware, **event envelope**.
- [x] Chuẩn hóa: API response format, error code, event envelope, correlation-id.
- [x] `docker-compose.yml` ban đầu: PostgreSQL, Redis, Kafka (KRaft).
- [x] `.env.example` + `appsettings` template.
- [x] Makefile/scripts: `up`, `down`, `logs`, `test`, `loadtest`. (Cân nhắc thêm **.NET Aspire AppHost** để orchestrate local.)

**Tiêu chí hoàn thành:** `docker compose up -d` dựng được hạ tầng local; solution build sạch.

---

### PHASE 0 — Product/Catalog Service (.NET)
**1–2 tuần · DB:** product_db

**Mục tiêu:** làm tốt một service trước khi phân tán.

**Việc cần làm**
- [x] Khởi tạo ASP.NET Core Web API + EF Core + Npgsql, kết nối Postgres.
- [x] Bảng: `products`, `categories`, `brands`, `product_variants`, `product_images`.
- [x] CRUD product / category / brand.
- [x] Product list: paging, search cơ bản, filter theo category/brand, sort theo price/name/createdAt.
- [x] Product detail, soft delete.
- [x] **FluentValidation** + exception middleware chuẩn hóa lỗi (ProblemDetails / ApiResponse).
- [x] **Giá lưu bằng `decimal`** → `NUMERIC(19,4)`, không dùng float/double. Xem [ADR-0001](adr/0001-tien-te-dung-decimal.md).
- [x] Health check `/health` + `/ready` (ASP.NET Core HealthChecks).
- [x] Structured logging JSON (Serilog).
- [x] Dockerfile multi-stage + seed data (EF migration + seeder).

**Lưu ý:** Product **không** có cột tồn kho thật. Chỉ: name, description, price, sku, brand, category, images, attributes, status.

**Tiêu chí hoàn thành:** CRUD + list/search/filter/sort chạy qua Docker, có seed, có health check, log JSON.

---

### PHASE 1 — Auth Service (.NET + OpenIddict)
**2 tuần · DB:** auth_db

**Mục tiêu:** tách Auth riêng thành **OIDC/OAuth2 provider chuẩn**; các service khác verify token qua JWKS. (eShop dùng Duende IdentityServer; ở đây dùng **OpenIddict** — OSS, miễn phí.)

**Việc cần làm**
- [ ] ASP.NET Core + EF Core + Npgsql + **OpenIddict** (server + validation).
- [ ] Bảng: `users`, `roles`, `user_roles` + bảng OpenIddict (applications, authorizations, scopes, tokens).
- [ ] Register + đăng nhập; cấp token theo **Authorization Code + PKCE** (web/mobile) và Client Credentials (service-to-service nếu cần).
- [ ] Hash password bằng BCrypt hoặc Argon2 (vd `BCrypt.Net-Next` / `Konscious.Security.Cryptography`).
- [ ] Access token (JWT, ký **RS256**) + refresh token + rotation; expose **`/.well-known/openid-configuration`** + **JWKS endpoint**.
- [ ] Role/scope: `CUSTOMER`, `ADMIN`; định nghĩa scope cho từng API.
- [ ] **Bảo mật:** rate-limit login, refresh token rotation, chống token reuse, revoke token.
- [ ] Product service: verify token bằng JWKS (`AddJwtBearer` trỏ authority về Auth); `[Authorize]` cho route ghi, chỉ `ADMIN` được ghi product (policy-based authorization).
- [ ] Health check, structured logging, Dockerfile.

**JWT claim tối thiểu:** `sub`, `email`, `roles`/`scope`, `jti`.

**Mấu chốt:** service khác **không cần biết secret** — chỉ verify chữ ký bằng public key lấy từ JWKS của Auth (RS256). Đây là điểm khác cốt lõi so với tự ký HS256 chia sẻ secret.

**Tiêu chí hoàn thành:** đăng nhập theo Authorization Code + PKCE → nhận access + refresh; dùng token tạo product; không token/không đủ scope/không phải ADMIN → 401/403; refresh + revoke hoạt động; service verify qua JWKS không cần secret chung.

---

### PHASE 1.5 — API Gateway bản minimal (.NET + YARP)
**3–7 ngày**

**Mục tiêu:** có một cửa vào duy nhất sớm.

**Việc cần làm**
- [ ] Cấu hình **YARP** reverse proxy: `/auth/**` → Auth, `/products/**` → Product.
- [ ] Correlation-id: nhận `X-Correlation-Id` hoặc tự sinh (middleware).
- [ ] Request logging (Serilog + YARP middleware).
- [ ] Verify JWT tại Gateway cho route private (`AddJwtBearer` trỏ `Authority` về Auth, validate qua JWKS).
- [ ] Forward user context: `X-User-Id`, `X-User-Email`, `X-User-Roles` (transform của YARP).
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
- [ ] Nghe event `UserRegistered` (từ Auth) → tạo profile rỗng tương ứng (consumer + inbox).
- [ ] Health check, logging, Dockerfile, Gateway route `/users/**`.

**Tiêu chí hoàn thành:** đăng ký ở Auth → User tự tạo profile qua event; sửa profile/địa chỉ chạy được.

> Đây là lần đầu hai service đồng bộ trạng thái qua **event**, không gọi trực tiếp.

---

### PHASE 3 — Cart Service (.NET) + sync call đầu tiên
**1 tuần · Store:** Redis (+ cart_db optional)

**Mục tiêu:** học Redis thực tế + xử lý gọi đồng bộ service-to-service.

**Việc cần làm**
- [ ] API: `GET /cart`, `POST /cart/items`, `PUT /cart/items/{variantId}`, `DELETE /cart/items/{variantId}`, `DELETE /cart`.
- [ ] Cart lưu Redis (StackExchange.Redis), key `cart:{userId}`, TTL 30 ngày.
- [ ] Khi add item: **gọi Product service** lấy snapshot (tên, variant, giá) lưu vào cart. Bắt đầu bằng typed `HttpClient` (REST), sau đó chuyển sang **gRPC** cho call nội bộ này (giống eShop Basket → Catalog) để so sánh latency + hợp đồng kiểu mạnh.
- [ ] **Xử lý lỗi sync call:** timeout + fallback khi Product chết — dùng **Microsoft.Extensions.Http.Resilience / Polly** (retry/timeout/circuit-breaker). Đây là điểm học chính.
- [ ] Validate quantity > 0.
- [ ] Health check, Dockerfile, Gateway route `/cart/**`.

**Tiêu chí hoàn thành:** add sản phẩm vào cart; cart nằm Redis; restart service không mất cart; Product chết thì Cart báo lỗi đẹp, không treo.

---

### PHASE 4 — Search Service (.NET) + Elasticsearch
**1–2 tuần · Store:** Elasticsearch

**Mục tiêu:** full-text search thật, khác hẳn filter SQL.

**Việc cần làm**
- [ ] ASP.NET Core + `Elastic.Clients.Elasticsearch`.
- [ ] Index sản phẩm: tên, mô tả, brand, category, attributes, price.
- [ ] API: full-text search, filter nhiều chiều, sort, paging, gợi ý (autocomplete/completion suggester).
- [ ] **Đồng bộ index qua event:** consume `ProductCreated`/`ProductUpdated`/`ProductDeleted` từ Product → cập nhật ES.
- [ ] Xử lý reindex toàn bộ (background job hoặc API admin).
- [ ] Health check, structured logging, Dockerfile, Gateway route `/search/**`.

**Tiêu chí hoàn thành:** search trả kết quả full-text + filter; sửa product ở Product service → vài giây sau search phản ánh đúng (eventual consistency qua event).

> Đây cũng là service đầu tiên **xây read-model riêng từ event** — một pattern quan trọng (CQRS nhẹ).

---

### PHASE 5 — Order + Inventory + Message Broker
**2–3 tuần · DB:** order_db, inventory_db · **Broker:** Kafka

**Mục tiêu:** event-driven thật sự — trái tim của microservices.

**Order (.NET — DDD + CQRS)**
- [ ] Bảng: `orders`, `order_items`, `order_status_histories`, `outbox_messages`, `inbox_messages`, `idempotency_keys`.
- [ ] **DDD chiến thuật:** `Order` là aggregate root (giữ invariant trạng thái + order item), `Address`/`Money` là value object, phát **domain event** khi đổi trạng thái (giống eShop Ordering).
- [ ] **CQRS bằng MediatR:** command (`CheckoutCommand`, `CancelOrderCommand`) tách khỏi query (`GetOrders`, `GetOrderById`); validation qua pipeline behavior (FluentValidation).
- [ ] API: `POST /orders/checkout`, `GET /orders`, `GET /orders/{id}`, `POST /orders/{id}/cancel`.
- [ ] Checkout: lấy cart snapshot → tạo order `PENDING` → lưu order item snapshot → ghi outbox `OrderCreated` (cùng transaction EF Core). Domain event → integration event được map khi commit.
- [ ] **Outbox publisher** (ban đầu chạy ngay trong Order như `BackgroundService`; tách sang Worker ở Phase 12): đọc outbox → publish (Kafka) → mark processed → retry nếu lỗi.

**Inventory (.NET)**
- [ ] Bảng: `inventory_items`, `stock_reservations`, `stock_movements`, `inbox_messages`, `outbox_messages`.
- [ ] Consume `OrderCreated` → reserve stock bằng **SQL atomic update** (chống oversell):
  ```sql
  UPDATE inventory_items
  SET available_quantity = available_quantity - @qty,
      reserved_quantity  = reserved_quantity  + @qty,
      updated_at = now()
  WHERE variant_id = @variantId AND available_quantity >= @qty;
  ```
  affected rows = 0 → thiếu hàng. (Dùng `ExecuteSqlInterpolatedAsync` của EF Core hoặc Dapper.)
- [ ] Publish `StockReserved` / `StockFailed`.
- [ ] **Idempotent consumer** dùng inbox table (Kafka at-least-once → message tới 2 lần).

> **Lựa chọn messaging:** dùng **MassTransit** (consumer, retry, in-memory/Kafka rider) cho năng suất, hoặc **Confluent.Kafka** trực tiếp để hiểu sâu offset/partition/commit. Nên thử Confluent.Kafka raw ít nhất một service rồi mới chuyển sang MassTransit.

**Tiêu chí hoàn thành:** checkout tạo order PENDING; Inventory tự reserve qua event; đủ hàng → StockReserved, thiếu → StockFailed; không service nào query DB service khác.

---

### PHASE 6 — Promotion/Pricing Service (.NET)
**1–2 tuần · DB:** promo_db

**Mục tiêu:** engine tính giá cuối — domain rule phức tạp.

**Việc cần làm**
- [ ] Bảng: `promotions`, `coupons`, `promotion_rules`, `coupon_redemptions`.
- [ ] API tính giá: nhận giỏ hàng → áp dụng rule (giảm %, giảm tiền, mua X tặng Y, freeship) → trả breakdown giá cuối (subtotal, discount, tax, shipping, total).
- [ ] Thiết kế **rule engine** rõ ràng (strategy/pipeline pattern; cân nhắc thư viện như `NRules` hoặc tự viết để học sâu).
- [ ] Validate coupon: hết hạn, hết lượt, điều kiện tối thiểu.
- [ ] **Tích hợp checkout:** Order gọi Promotion lúc checkout để chốt giá; lưu snapshot giá vào order.
- [ ] Chống lạm dụng coupon (idempotent redemption).
- [ ] Health check, logging, Dockerfile, Gateway route `/promotions/**`.

**Tiêu chí hoàn thành:** áp coupon hợp lệ → giá giảm đúng; coupon sai/hết hạn → từ chối; checkout phản ánh đúng giá đã giảm.

---

### PHASE 7 — Saga + Payment Service (.NET)
**2–3 tuần · DB:** payment_db

**Mục tiêu:** hoàn chỉnh luồng checkout + xử lý giao dịch phân tán.

**Payment (.NET — DDD + CQRS nhẹ)**
- [ ] Bảng: `payments`, `payment_logs`, `idempotency_keys`, `inbox_messages`, `outbox_messages`.
- [ ] `Payment` là aggregate (giữ invariant trạng thái thanh toán); command/handler qua MediatR; consumer là background processor (giống eShop `PaymentProcessor`).
- [ ] Consume `StockReserved` → tạo payment PENDING.
- [ ] Mock success/fail + API callback giả lập `POST /payments/mock-callback`.
- [ ] Publish `PaymentCompleted` / `PaymentFailed`.

**Order bổ sung**
- [ ] Consume `StockReserved`, `StockFailed`, `PaymentCompleted`, `PaymentFailed`.
- [ ] State machine: `PENDING → INVENTORY_RESERVED → PAYMENT_PENDING → PAID → CONFIRMED` / `CANCELLED` / `FAILED`. (Cân nhắc **MassTransit Saga State Machine / Automatonymous** hoặc tự viết để hiểu cơ chế.)
- [ ] Lưu `order_status_histories`.

**Inventory bổ sung**
- [ ] Consume `PaymentFailed` → release stock → publish `StockReleased`.

**⭐ Bổ khuyết quan trọng (đừng bỏ):**
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

> Phần khó nhất, phân biệt middle với junior. Nên dành thời gian cho nó.

---

### PHASE 8 — Realtime WebSocket (.NET + SignalR)
**1–2 tuần · Store:** Redis backplane

**Mục tiêu:** đẩy trạng thái đơn hàng realtime; scale qua nhiều instance.

**Việc cần làm**
- [ ] Notification service: **SignalR Hub**; client connect bằng JWT; map connection theo `userId` (dùng `IUserIdProvider`).
- [ ] Group theo `user:{userId}`, `order:{orderId}`.
- [ ] Consumer nhận `OrderConfirmed`/`OrderCancelled` → push qua SignalR; dùng **Redis backplane** (`AddStackExchangeRedis`) để nhiều instance đồng bộ connection (giải bài toán WebSocket stateful khi scale).
- [ ] Lưu notification vào `notification_db` (lịch sử).
- [ ] Chạy 2+ instance Notification để test scale.
- [ ] Gateway route WebSocket (YARP hỗ trợ WebSocket passthrough) + k6 WebSocket test cơ bản.

**Tiêu chí hoàn thành:** checkout xong nhận status realtime; 2 instance vẫn nhận đúng; disconnect/reconnect không lỗi.

---

### PHASE 9 — Media Service + Gateway nâng cao + Cache + Resilience
**2 tuần · Ngôn ngữ:** .NET (Media + Gateway)

**Mục tiêu:** ảnh sản phẩm + biến Gateway thành lớp bảo vệ.

**Media (.NET)**
- [ ] Upload ảnh → lưu MinIO/S3 (`AWSSDK.S3` hoặc `Minio`); trả URL.
- [ ] Resize/thumbnail (tùy chọn, vd `ImageSharp`); validate file type/size.
- [ ] Product tham chiếu URL ảnh từ Media.

**Gateway nâng cao**
- [ ] Rate-limit bằng Redis (theo IP + theo userId) — kết hợp middleware `RateLimiter` của .NET / YARP.
- [ ] Cache-aside cho product list/detail (`IDistributedCache` + Redis); **invalidate khi `ProductUpdated`**.
- [ ] Timeout per route; retry chỉ cho GET; **circuit breaker** (Polly / resilience handler).
- [ ] Body size limit, CORS, compression (tùy chọn).
- [ ] Metrics cơ bản: request count, latency, status code (OpenTelemetry).

**Tiêu chí hoàn thành:** upload ảnh & gắn vào product; vượt rate-limit → 429; product detail lần 2 nhanh hơn nhờ cache; product update xóa cache đúng; service phía sau chết → Gateway fail fast.

---

### PHASE 10 — Observability chuẩn
**1–2 tuần (nhưng làm dần từ Phase 0)**

**Mục tiêu:** debug được hệ phân tán.

**Việc cần làm**
- [ ] **OpenTelemetry .NET SDK** cho mọi service (auto-instrumentation ASP.NET Core, HttpClient, EF Core, Kafka).
- [ ] Trace HTTP request, DB query, broker publish/consume.
- [ ] **Propagate trace context qua Kafka headers** (xuyên service) — dùng W3C TraceContext propagator.
- [ ] Prometheus metrics (OpenTelemetry exporter) + Grafana dashboard cho mọi service + Postgres + Redis + Kafka.
- [ ] Loki (logs, qua Serilog sink) + Tempo/Jaeger (traces, OTLP exporter).

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
- [ ] Tối ưu: index, cache, connection pool, **PgBouncer**, consumer concurrency, batch outbox, **read replica**, horizontal scale. (Cộng các đặc thù .NET: pooling `DbContext`, `Server GC`, compiled queries EF Core, `System.Text.Json` source-gen.)

**Tiêu chí hoàn thành:** trả lời được — bottleneck ở đâu, đã tối ưu gì, trước/sau khác nhau bao nhiêu, p95/p99 & error rate là bao nhiêu.

---

### PHASE 12 — Worker Service (.NET) tách riêng
**1 tuần · Project type:** .NET Worker Service

**Mục tiêu:** gom các tác vụ nền vào service chuyên trách (trước đó chạy nhúng trong từng service).

**Việc cần làm**
- [ ] Outbox publisher tập trung (đọc outbox của các service → publish) bằng `BackgroundService`.
- [ ] Scheduler/cron: dọn idempotency key cũ, dọn cart hết hạn (cân nhắc **Quartz.NET** hoặc `PeriodicTimer`).
- [ ] **Saga timeout job:** quét order treo quá deadline → trigger compensation.
- [ ] **Reservation expiry job:** quét stock reservation quá hạn → release.

**Tiêu chí hoàn thành:** các job nền chạy ổn định, độc lập; order treo & stock giam được tự xử lý.

---

### PHASE 13 — Kubernetes, CI/CD, Broker nâng cao
**3–5 tuần**

**Kubernetes**
- [ ] Hoàn thiện Dockerfile mọi service (multi-stage, base `mcr.microsoft.com/dotnet/aspnet:10.0`); docker-compose full local.
- [ ] Manifest: Deployment, Service, ConfigMap, Secret, Ingress, **HPA**, PVC nếu cần.
- [ ] Liveness/readiness probe (map vào `/health` `/ready`); resource requests/limits; namespace.
- [ ] Helm hoặc Kustomize; deploy lên kind/minikube/k3d; test HPA bằng load.

**CI/CD (GitHub Actions)**
- [ ] Pipeline .NET chung: `dotnet restore` / `build` / `test` / `format --verify-no-changes` / `publish`.
- [ ] Build Docker image; security scan (tùy chọn, vd `dotnet list package --vulnerable` + Trivy); migration check; PR fail CI → không merge.

**Broker nâng cao**
- [ ] Kafka: nhiều partition, consumer group scaling, **retry topic + DLQ**, event replay, Schema Registry (tùy chọn).
- [ ] (Tùy chọn) Làm nhánh RabbitMQ: topic exchange, quorum queue, DLX, prefetch tuning — để so sánh với Kafka (MassTransit hỗ trợ cả hai transport).

**Tiêu chí hoàn thành:** `kubectl apply` chạy toàn hệ thống; Ingress expose Gateway; tăng tải → pod tự scale; PR phải qua CI mới merge; message lỗi vào DLQ thay vì chặn partition.

---

### PHASE NÂNG CAO (tùy chọn) — học theo eShop hiện đại

Các phase này không bắt buộc cho luồng mua hàng, nhưng bám sát những thứ eShop mới đang làm.

**A. Webhooks Service (.NET)**
- [ ] Cho phép bên thứ ba đăng ký webhook (vd `OrderConfirmed`, `PaymentCompleted`).
- [ ] Consume integration event → gọi HTTP callback đã đăng ký, có retry + ký HMAC payload.
- [ ] Bảng: `webhook_subscriptions`, `webhook_deliveries`.

**B. AI Semantic Search (trong Product hoặc Search)**
- [ ] Sinh embedding cho sản phẩm (`Microsoft.Extensions.AI`, provider OpenAI/Azure OpenAI hoặc Ollama local).
- [ ] Lưu vector bằng **pgvector** (Postgres) hoặc dùng vector của Elasticsearch.
- [ ] API tìm kiếm ngữ nghĩa (vector similarity) + "shopping assistant" hỏi-đáp — đúng đặc sản eShop.

**C. BFF / Aggregator Gateway (.NET + YARP)**
- [ ] Thêm BFF cho client (web/mobile) ngoài Gateway passthrough: aggregate nhiều call (vd product + giá + tồn) thành một response, transform theo client (giống `mobile-bff` của eShop).

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

> Định nghĩa envelope một lần ở `shared/Ecommerce.Messaging` (record C#) và tái dùng cho mọi service — lợi thế lớn của one-stack.

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

> Mỗi service có `DbContext` + migration EF Core riêng. Tuyệt đối không share `DbContext` hay schema giữa service.

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

> Triển khai `ApiResponse<T>` + global exception middleware ở `shared/Ecommerce.BuildingBlocks`, áp cho mọi service.

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
- [ ] Có health check + logging có cấu trúc (Serilog).
- [ ] Có migration/schema rõ ràng (EF Core) + seed nếu cần.
- [ ] Có test cơ bản (xUnit; integration test với Testcontainers nếu hợp).
- [ ] Có API/event contract trong `contracts/`.
- [ ] Không hardcode secret (dùng appsettings + env + User Secrets cho dev).
- [ ] Có API versioning (`/v1/` — `Asp.Versioning`).
- [ ] Commit Git riêng.

---

## 11. THỨ TỰ & LỘ TRÌNH THỜI GIAN

```
Phase -1  Foundation
Phase 0   Product            (.NET)
Phase 1   Auth               (.NET)
Phase 1.5 Gateway mỏng       (.NET + YARP)
Phase 2   User               (.NET)
Phase 3   Cart               (.NET)      — sync call + Redis
Phase 4   Search             (.NET)      — ES + read-model từ event
Phase 5   Order + Inventory  (.NET)      — Kafka, Outbox/Inbox
Phase 6   Promotion/Pricing  (.NET)
Phase 7   Saga + Payment     (.NET)      — + saga timeout + reservation expiry
Phase 8   WebSocket          (.NET + SignalR)
Phase 9   Media + Gateway nâng cao (.NET)
Phase 10  Observability
Phase 11  Load Test
Phase 12  Worker tách riêng  (.NET Worker Service)
Phase 13  Kubernetes + CI/CD + Broker nâng cao
```

**Core tối thiểu để có một ecommerce đúng bản chất:** `-1 → 0 → 1 → 1.5 → 3 → 5 → 7`. Làm xong là đã có luồng mua hàng đầu-cuối chạy được. Các phase còn lại bồi đắp cho đầy đủ.

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

**Tổng ước lượng (bán thời gian):** vì chỉ còn một stack, ước lượng có thể rút ngắn so với bản đa ngôn ngữ — khoảng **5–7 tháng** cho toàn bộ 13 service. Đây vẫn là dự án lớn — đi từ từ, mỗi phase một mục tiêu, commit đều.

---

## ĐỐI CHIẾU VỚI eShop (Microsoft)

Tham chiếu `dotnet/eShop` (bản .NET Aspire, broker RabbitMQ/Azure Service Bus, AI semantic search). Chỉ xét backend.

**Đã học theo eShop (đưa vào doc):**
- DDD chiến thuật + CQRS bằng MediatR cho service giao dịch (Order, Payment).
- OIDC/OAuth2 provider chuẩn (OpenIddict) + verify qua JWKS, thay vì tự ký HS256 chia sẻ secret.
- gRPC cho sync call nội bộ tần suất cao (Cart → Product).
- .NET Aspire làm orchestrator local + project `ServiceDefaults` gói cross-cutting.
- Resilience chuẩn hoá bằng `Microsoft.Extensions.Http.Resilience`.
- Phase nâng cao tùy chọn: Webhooks, AI semantic search (pgvector + `Microsoft.Extensions.AI`), BFF/aggregator gateway.

**Chủ động khác eShop (giữ vì hợp mục tiêu học sâu microservices):**
- **Broker: Kafka** (partition, consumer group, replay, DLQ topic) thay vì RabbitMQ — nhiều thứ để học hơn. RabbitMQ để ở nhánh so sánh.
- **Inventory là service riêng** với reserve/release, atomic update chống oversell, reservation expiry — eShop để stock trong Catalog, không có reservation. Bản này sát thực tế thương mại hơn.
- **Saga đầy đủ** (timeout + compensation + orphan handling) thay vì grace-period process manager của eShop.
- **Search riêng + Elasticsearch** (read-model từ event) bên cạnh khả năng thêm semantic search.
- **Promotion/Pricing là service riêng** với rule engine — eShop không có.
- **Inbox + idempotency + DLQ/retry topic tường minh**, observability tự host (Prometheus/Grafana/Loki/Tempo), và load test + SLO bằng k6 — eShop không nhấn mạnh các phần này.

---

## GHI CHÚ CUỐI

- **Một stack duy nhất — .NET 10:** Gateway (YARP), Notification (SignalR), Worker (Worker Service), còn lại là ASP.NET Core Web API. Lợi thế: tái dùng `shared/` tối đa, một CI/CD, một bộ tooling, refactor xuyên service dễ.
- **Đừng để one-stack làm mờ ranh giới service:** vẫn Database per Service, vẫn giao tiếp qua API/event, `shared/` chỉ chứa building block kỹ thuật — không chứa domain.
- **Các điểm "đặc sản" .NET nên học sâu:** DDD + CQRS (MediatR), OpenIddict (OIDC/OAuth2), EF Core (migration, atomic update, compiled query), gRPC nội bộ, YARP, SignalR + Redis backplane, MassTransit vs Confluent.Kafka, resilience handler/Polly, OpenTelemetry .NET, Worker Service / BackgroundService, .NET Aspire + ServiceDefaults.
- **Hai bổ khuyết logic quan trọng nhất** vẫn nằm ở **Phase 7**: saga timeout và reservation expiry — đừng bỏ, đó là thứ làm saga "thật".
- Mỗi pattern (Outbox, Inbox, Saga, CQRS read-model, cache invalidation) sau khi làm xong nên **tự viết lại bằng lời của mình** để học sâu.
