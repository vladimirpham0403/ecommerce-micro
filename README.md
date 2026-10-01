# E-commerce Microservices

Dự microservices thuần **.NET 10**. Xem [docs/architecture.md](docs/architecture.md).

## Yêu cầu
- Docker + Docker Compose
- .NET SDK 10 (xem `global.json`) — cần để build và chạy `dotnet test`
- (Tùy chọn) .NET Aspire workload để orchestrate local

## Chuẩn bị

**1. Hosts file.** Auth phát token với issuer `http://auth:5044`. Trình duyệt trên máy và container
phải cùng gọi được đúng URL đó, nếu không verify chữ ký sẽ trượt. Thêm một dòng (cần quyền admin):

```
# C:\Windows\System32\drivers\etc\hosts   (Linux/macOS: /etc/hosts)
127.0.0.1 auth
```

**2. Chứng thư ký token.** Không commit vào repo - mỗi máy tự sinh:

```bash
cp .env.example .env
make certs
```

## Chạy hạ tầng local
```bash
make up        # hoặc ./scripts/up.sh
make ps        # kiểm tra container healthy
make logs      # xem log
make down      # tắt
```

Sau khi `up`:
- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`
- Kafka: `localhost:29092` (host) / `kafka:9092` (trong Docker)
- Kafka UI: http://localhost:8080
- Product API: http://localhost:5033 — Swagger tại http://localhost:5033/swagger
- Auth API: http://auth:5044 — discovery tại http://auth:5044/.well-known/openid-configuration

Tài khoản dev (chỉ seed ở `Development`): `admin@ecom.local` / `Admin@123456`và `customer@ecom.local` / `Customer@123456`.

## Cấu trúc
- `contracts/` - chuẩn dùng chung (event, error, openapi).
- `infra/` - docker-compose, kafka, monitoring.
- `services/` - các microservice.
- `scripts/` - tiện ích chạy local.