# E-commerce Microservices

Dự microservices thuần **.NET 10**. Xem [docs/architecture.md](docs/architecture.md).

## Yêu cầu
- Docker + Docker Compose
- .NET SDK 10 (xem `global.json`) — cần để build và chạy `dotnet test`
- (Tùy chọn) .NET Aspire workload để orchestrate local

## Chạy hạ tầng local
```bash
cp .env.example .env
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

## Cấu trúc
- `contracts/` - chuẩn dùng chung (event, error, openapi).
- `infra/` - docker-compose, kafka, monitoring.
- `services/` - các microservice.
- `scripts/` - tiện ích chạy local.