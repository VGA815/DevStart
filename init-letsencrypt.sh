#!/bin/sh
# Bootstrap a Let's Encrypt certificate for the nginx + certbot compose stack.
#
# Run ONCE on the production host, AFTER you have:
#   1. Filled .env (DOMAIN and LETSENCRYPT_EMAIL at minimum).
#   2. Replaced `example.com` in config/nginx/nginx.prod.conf with the SAME domain as DOMAIN.
#   3. Pointed DNS for $DOMAIN at this host (ports 80/443 reachable from the internet).
#
# Usage:  ./init-letsencrypt.sh
set -eu

COMPOSE="docker compose -f docker-compose.prod.yml"
RSA_KEY_SIZE=4096

# Read DOMAIN / LETSENCRYPT_EMAIL from .env WITHOUT executing it. A .env is not a shell
# script: `. ./.env` would choke on a pasted secret containing $, #, spaces or quotes.
if [ -f .env ]; then
  while IFS= read -r _line || [ -n "$_line" ]; do
    case "$_line" in
      DOMAIN=*)            DOMAIN=${_line#DOMAIN=} ;;
      LETSENCRYPT_EMAIL=*) LETSENCRYPT_EMAIL=${_line#LETSENCRYPT_EMAIL=} ;;
    esac
  done < .env
fi

# Tolerate CRLF .env files edited on Windows.
DOMAIN=$(printf '%s' "${DOMAIN:-}" | tr -d '\r')
LETSENCRYPT_EMAIL=$(printf '%s' "${LETSENCRYPT_EMAIL:-}" | tr -d '\r')

: "${DOMAIN:?Set DOMAIN in .env}"
: "${LETSENCRYPT_EMAIL:?Set LETSENCRYPT_EMAIL in .env}"

CERT_PATH="/etc/letsencrypt/live/$DOMAIN"

# (Re)create a 1-day self-signed cert so nginx can BOOT and serve :80. Without a cert
# file at this path nginx crash-loops ("[emerg] cannot load certificate"), nothing
# listens on :80, and the ACME challenge can't be served — which Let's Encrypt reports
# as "connection refused". So nginx must never be left without a cert here.
make_dummy() {
  $COMPOSE run --rm --entrypoint sh certbot -c \
    "mkdir -p '$CERT_PATH' && openssl req -x509 -nodes -newkey rsa:$RSA_KEY_SIZE -days 1 \
       -keyout '$CERT_PATH/privkey.pem' -out '$CERT_PATH/fullchain.pem' -subj /CN=localhost"
}

echo "### 1/6 Temporary self-signed certificate for $DOMAIN ..."
make_dummy

echo "### 2/6 Starting nginx ..."
$COMPOSE up -d --force-recreate nginx

echo "### 3/6 Waiting until nginx serves the ACME challenge on :80 ..."
$COMPOSE run --rm --entrypoint sh certbot -c \
  'mkdir -p /var/www/certbot/.well-known/acme-challenge && echo ok > /var/www/certbot/.well-known/acme-challenge/_probe'
if command -v curl >/dev/null 2>&1; then
  i=0
  until curl -fsS "http://localhost/.well-known/acme-challenge/_probe" >/dev/null 2>&1; do
    i=$((i + 1))
    if [ "$i" -ge 15 ]; then
      echo "ERROR: nginx is not serving http://localhost/.well-known/... on :80 (crash-looping?)." >&2
      echo "       Inspect:  docker ps --filter name=nginx   and   docker logs <nginx container>" >&2
      exit 1
    fi
    sleep 2
  done
  echo "    OK on :80 locally."
else
  echo "    (curl not found — skipping local self-check)"
fi
echo "    Ensure http://$DOMAIN/ is also reachable from the INTERNET on :80"
echo "    (DNS -> this host; ports 80/443 open in any cloud/provider firewall)."

echo "### 4/6 Removing the temporary certificate (so certbot writes a clean lineage) ..."
$COMPOSE run --rm --entrypoint sh certbot -c \
  "rm -rf '/etc/letsencrypt/live/$DOMAIN' '/etc/letsencrypt/archive/$DOMAIN' '/etc/letsencrypt/renewal/$DOMAIN.conf'"

echo "### 5/6 Requesting the real Let's Encrypt certificate for $DOMAIN ..."
if $COMPOSE run --rm --entrypoint certbot certbot \
     certonly --webroot -w /var/www/certbot \
       --email "$LETSENCRYPT_EMAIL" -d "$DOMAIN" \
       --rsa-key-size "$RSA_KEY_SIZE" --agree-tos --no-eff-email --force-renewal; then
  echo "### 6/6 Reloading nginx with the real certificate ..."
  $COMPOSE exec nginx nginx -s reload
  echo "### Done. Bring up the full stack with:"
  echo "    $COMPOSE up -d"
else
  echo "ERROR: certbot failed (see its output above). Restoring a self-signed cert so nginx" >&2
  echo "       keeps serving instead of crash-looping; fix the cause, then re-run this script." >&2
  make_dummy
  $COMPOSE up -d --force-recreate nginx
  echo "       Most common cause: http://$DOMAIN/ not reachable from the internet on :80." >&2
  exit 1
fi
