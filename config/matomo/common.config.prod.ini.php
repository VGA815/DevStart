; <?php exit; ?> DO NOT REMOVE THIS LINE
; ^ Matomo convention: the file has a .php extension, so if it is ever requested over HTTP, PHP
;   parses it and exits here instead of dumping the settings. Everything below is plain INI.
;
; PRODUCTION overrides for Matomo's global.ini.php. Mounted read-only at
;   /var/www/html/config/common.config.ini.php
;
; Matomo loads global.ini.php -> this file -> config/config.ini.php, and the install wizard and
; admin UI only ever write config.ini.php — so what is set here survives install and upgrades.
;
; !! THIS FILE MUST EXIST AND BE MOUNTED BEFORE THE INSTALL WIZARD IS FIRST OPENED. Without
;    proxy_uri_header the wizard's own pages emit URLs that drop the /matomo prefix and the
;    install cannot be completed. If that happens: `docker compose -f docker-compose.prod.yml
;    down`, remove the matomo-data and matomo-db-data volumes, and start over.
;
; !! And, as with config/nginx/.htpasswd (DEPLOYMENT.md): if this host path is missing, Docker
;    silently creates a DIRECTORY there and Matomo fails to boot.
;
; MIRROR of common.config.ini.php (dev) — change both together.

[General]
; nginx serves Matomo at https://<DOMAIN>/matomo/ and STRIPS the prefix
; (rewrite ^/matomo/(.*)$ /$1 break; — NOT a trailing slash on proxy_pass, which would blank out
; the request URI because the upstream is a variable), then hands the prefix back
; via `proxy_set_header X-Forwarded-Uri /matomo`. proxy_uri_header makes Matomo prepend that
; value when it builds its own script name, so logins, redirects and asset URLs keep /matomo.
; Remove either half and every absolute URL Matomo emits loses the prefix — the classic failure
; mode of a path-mounted Matomo.
proxy_uri_header = 1
proxy_client_headers[] = HTTP_X_FORWARDED_FOR
proxy_host_headers[] = HTTP_X_FORWARDED_HOST
; Exactly one trusted proxy (nginx). $proxy_add_x_forwarded_for appends the real peer address to
; whatever the client sent, so the LAST entry is the only one a visitor cannot forge by hand.
; Without this a visitor can fake their location with a handwritten X-Forwarded-For.
proxy_ip_read_last_in_list = 1

; TLS terminates at nginx; the container only ever sees plain http on port 80.
; NOTE: force_ssl is also exposed in Administration -> General settings. Toggling it there writes
; config.ini.php, which is loaded AFTER this file and therefore wins.
assume_secure_protocol = 1
force_ssl = 1

; Archiving runs in the matomo-archive sidecar (`console core:archive`). With browser triggering
; left on, the first report view of the day blocks for minutes.
; NOTE: also exposed in Administration -> General settings as "Archive reports when viewed from
; the browser" — a UI toggle lands in config.ini.php and overrides this line.
enable_browser_archiving_triggering = 0
