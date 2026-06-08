#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

if [ ! -f .env ]; then
  echo "Chưa có .env — copy từ .env.example"
  cp .env.example .env
fi

docker compose -f infra/docker-compose.yml up -d
echo "Đang chờ hạ tầng healthy..."
docker compose -f infra/docker-compose.yml ps