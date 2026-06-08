COMPOSE = docker compose --env-file .env -f infra/docker-compose.yml

.PHONY: up down logs ps restart test loadtest clean help

help:
	@echo "up        - Dựng hạ tầng local (Postgres, Redis, Kafka)"
	@echo "down      - Tắt hạ tầng"
	@echo "logs      - Xem log realtime"
	@echo "ps        - Trạng thái container"
	@echo "restart   - Tắt rồi bật lại"
	@echo "clean     - Tắt + xóa volume (mất dữ liệu)"
	@echo "test      - Chạy test (placeholder cho phase sau)"
	@echo "loadtest  - Chạy k6 (placeholder cho phase sau)"

up:
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
	@echo "Chưa có service nào"

loadtest:
	@echo "Chưa có k6 script"