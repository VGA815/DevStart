#!/bin/sh
# Generate strong random secrets for DevStart's production .env.
#
# Prints KEY=value lines to STDOUT (paste them into .env), and guidance to STDERR
# (so stdout stays clean/paste-able). It does NOT touch .env itself.
#
#   ./gen-secrets.sh              # print to console
#   ./gen-secrets.sh >> .env      # append straight into .env
#
# Values are hex (0-9a-f): safe in .env and inside connection strings — no quoting,
# no $ / # / spaces / base64 padding to trip up the parser.
set -eu

if ! command -v openssl >/dev/null 2>&1; then
  echo "error: openssl not found. Install it (Linux: apt/dnf install openssl; Windows: use Git Bash)." >&2
  exit 1
fi

# gen <bytes> -> 2*<bytes> hex chars. 32 bytes = 64 hex = 256-bit.
gen() { openssl rand -hex "${1:?usage: gen <bytes>}"; }

cat <<EOF
# --- DevStart generated secrets ($(date -u '+%Y-%m-%dT%H:%M:%SZ')) ---
# Postgres
POSTGRES_PASSWORD=$(gen 24)

# Redis
REDIS_PASSWORD=$(gen 24)

# MinIO / S3 object storage (root user is an access key; any string works)
MINIO_ROOT_USER=$(gen 12)
MINIO_ROOT_PASSWORD=$(gen 24)

# JWT — HS256 needs >=32 chars; 64 hex = 256-bit
JWT_SECRET=$(gen 32)

# Grafana
GRAFANA_ADMIN_PASSWORD=$(gen 12)

# Hangfire
HANGFIRE_DASHBOARD_SECRET=$(gen 32)

# Centrifugo (real-time). TokenHmacSecret is shared by the API and Centrifugo via
# this same variable, so they stay in sync automatically.
CENTRIFUGO_API_KEY=$(gen 24)
CENTRIFUGO_TOKEN_HMAC_SECRET=$(gen 32)
CENTRIFUGO_ADMIN_PASSWORD=$(gen 16)
CENTRIFUGO_ADMIN_SECRET=$(gen 24)
EOF

cat >&2 <<'EOF'

[gen-secrets] Done. The lines above are secrets only (stdout).
Still fill these NON-secret values in .env by hand:
  DOMAIN, LETSENCRYPT_EMAIL, JWT_ISSUER, JWT_AUDIENCE, JWT_EXPIRATION_MINUTES,
  OAUTH_*, SMTP_*, YOOKASSA_*, MINIO_BUCKET, MINIO_PUB_ENDPOINT, MINIO_PUB_USE_SSL,
  MINIO_USE_SSL, FORWARDED_KNOWN_NETWORKS.
Never commit .env. Regenerate and rotate if a secret ever leaks.
EOF
