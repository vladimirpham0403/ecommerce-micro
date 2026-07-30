# Kafka (KRaft) - ghi chú cho môi trường dev

## Phân biệt listener
- Service chạy TRONG Docker network -> `kafka:9092` (INTERNAL).
- Máy host (script, k6, IDE) -> `localhost:29092` (EXTERNAL).

## Lệnh hay dùng (chạy trong container)
```bash
# Liệt kê topic
docker exec ecom-kafka kafka-topics.sh --bootstrap-server localhost:9092 --list

# Tạo topic
docker exec ecom-kafka kafka-topics.sh --bootstrap-server localhost:9092 \
  --create --topic order.events --partitions 3 --replication-factor 1

# Consume thử
docker exec ecom-kafka kafka-console-consumer.sh --bootstrap-server localhost:9092 \
  --topic order.events --from-beginning
```

## Kafka UI
Mở http://localhost:8080 để xem topic/message trực quan.

## Lưu ý
- Dev để `AUTO_CREATE_TOPICS_ENABLE=true` cho tiện. Production nên tắt và tạo topic chủ động.
- `replication-factor=1` vì chỉ 1 broker local.