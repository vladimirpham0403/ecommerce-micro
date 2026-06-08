# E-commerce Microservices

Dự án học microservices đa ngôn ngữ (Go, .NET, NestJS, Java). Xem [docs/architecture.md](docs/architecture.md).

## Yêu cầu
- Docker + Docker Compose
- (Sau Phase 0) Go, .NET SDK, Node.js, JDK tùy service

## Chạy hạ tầng local (Phase -1)
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

## Cấu trúc
- `contracts/` — chuẩn dùng chung (event, error, openapi).
- `infra/` — docker-compose, kafka, monitoring.
- `services/` — các microservice.
- `scripts/` — tiện ích chạy local.

## Lộ trình
Phase -1 (nền) -> 0 (Product) -> 1 (Auth) -> ... Xem architecture.md mục 11.