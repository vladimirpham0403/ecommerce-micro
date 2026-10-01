#!/usr/bin/env bash
# Sinh cặp chứng thư cho Auth service: một để ký token, một để mã hóa token.
#
# Hai cert riêng biệt là có chủ đích: đổi khóa ký không được kéo theo việc giết sạch
# refresh token đang lưu hành (chúng được mã hóa bằng khóa encryption).
#
# Chạy một lần trước `make up` đầu tiên. File .pfx bị .gitignore loại trừ.
set -euo pipefail

# Git Bash/MSYS trên Windows tưởng "/CN=..." là đường dẫn và tự đổi thành C:/Program Files/...
# Biến này tắt hành vi đó; trên Linux/macOS nó vô hại vì không ai đọc tới.
export MSYS_NO_PATHCONV=1

CERT_DIR="${CERT_DIR:-infra/certs}"
DAYS="${DAYS:-730}"

if [[ -z "${AUTH_CERT_PASSWORD:-}" ]]; then
  echo "Thiếu AUTH_CERT_PASSWORD. Ví dụ:" >&2
  echo "  AUTH_CERT_PASSWORD=auth_cert_dev_password ./scripts/gen-auth-certs.sh" >&2
  exit 1
fi

mkdir -p "$CERT_DIR"

for kind in signing encryption; do
  if [[ -f "$CERT_DIR/$kind.pfx" ]]; then
    echo "Bỏ qua $kind.pfx (đã tồn tại)."
    continue
  fi

  openssl req -x509 -newkey rsa:2048 -nodes -sha256 -days "$DAYS" -subj "/CN=ecommerce-auth-$kind" -keyout "$CERT_DIR/$kind.key" -out "$CERT_DIR/$kind.crt"
  openssl pkcs12 -export -inkey "$CERT_DIR/$kind.key" -in "$CERT_DIR/$kind.crt" -out "$CERT_DIR/$kind.pfx" -passout pass:"$AUTH_CERT_PASSWORD"

  # Chỉ giữ PFX - .key là private key trần, không có lý do gì để nó nằm lại trên đĩa.
  rm -f "$CERT_DIR/$kind.key" "$CERT_DIR/$kind.crt"

  echo "Đã tạo $CERT_DIR/$kind.pfx"
done
