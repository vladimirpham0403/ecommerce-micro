COMPOSE = docker compose --env-file .env -f infra/docker-compose.yml

.PHONY: up down logs ps restart test loadtest clean certs help

help:
	@echo "certs     - Sinh chứng thư ký/mã hóa token cho Auth (chạy trước 'up' lần đầu)"
	@echo "up        - Dựng hạ tầng local (Postgres, Redis, Kafka)"
	@echo "down      - Tắt hạ tầng"
	@echo "logs      - Xem log realtime"
	@echo "ps        - Trạng thái container"
	@echo "restart   - Tắt rồi bật lại"
	@echo "clean     - Tắt + xóa volume (mất dữ liệu)"
	@echo "test      - Chạy test (placeholder cho phase sau)"
	@echo "loadtest  - Chạy k6 (placeholder cho phase sau)"

certs:
	set -a && . ./.env && set +a && ./scripts/gen-auth-certs.sh

# Auth không khởi động nổi nếu thiếu PFX, nên chặn sớm ở đây cho dễ hiểu hơn là để container crash.
up:
	@test -f infra/certs/signing.pfx || { echo "Thiếu infra/certs/signing.pfx - chạy 'make certs' trước."; exit 1; }
	$(COMPOSE) up -d

down:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs -f

ps:
	$(COMPOSE) ps

restart: down up

clean:
	$(COMPOSE) down -v

test:
	dotnet test

loadtest:
	@echo "Chưa có k6 script"