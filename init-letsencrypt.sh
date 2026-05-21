#!/bin/sh
# Bootstrap a Let's Encrypt certificate for the nginx + certbot compose stack.
#
# Run ONCE on the production host, AFTER you have:
#   1. Filled .env (DOMAIN and LETSENCRYPT_EMAIL at minimum).
#   2. Replaced `api.example.com` in config/nginx/nginx.prod.conf with the SAME domain as DOMAIN.
#   3. Pointed DNS for $DOMAIN at this host (ports 80/443 reachable from the internet).
#
# Usage:  ./init-letsencrypt.sh
set -eu

COMPOSE="docker compose -f docker-compose.prod.yml"
RSA_KEY_SIZE=4096

if [ -f .env ]; then
  # shellcheck disable=SC1091
  . ./.env
fi

: "${DOMAIN:?Set DOMAIN in .env}"
: "${LETSENCRYPT_EMAIL:?Set LETSENCRYPT_EMAIL in .env}"

CERT_PATH="/etc/letsencrypt/live/$DOMAIN"

echo "### 1/5 Creating a temporary self-signed certificate for $DOMAIN ..."
$COMPOSE run --rm --entrypoint "\
  sh -c 'mkdir -p $CERT_PATH && \
  openssl req -x509 -nodes -newkey rsa:$RSA_KEY_SIZE -days 1 \
    -keyout $CERT_PATH/privkey.pem \
    -out $CERT_PATH/fullchain.pem \
    -subj /CN=localhost'" certbot

echo "### 2/5 Starting nginx (serves the ACME challenge on :80) ..."
$COMPOSE up -d nginx

echo "### 3/5 Removing the temporary certificate ..."
$COMPOSE run --rm --entrypoint "\
  rm -rf /etc/letsencrypt/live/$DOMAIN \
         /etc/letsencrypt/archive/$DOMAIN \
         /etc/letsencrypt/renewal/$DOMAIN.conf" certbot

echo "### 4/5 Requesting the real Let's Encrypt certificate for $DOMAIN ..."
$COMPOSE run --rm --entrypoint "\
  certbot certonly --webroot -w /var/www/certbot \
    --email $LETSENCRYPT_EMAIL \
    -d $DOMAIN \
    --rsa-key-size $RSA_KEY_SIZE \
    --agree-tos --no-eff-email --force-renewal" certbot

echo "### 5/5 Reloading nginx with the real certificate ..."
$COMPOSE exec nginx nginx -s reload

echo "### Done. Bring up the full stack with:"
echo "    $COMPOSE up -d"
