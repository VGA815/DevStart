; <?php exit; ?> DO NOT REMOVE THIS LINE
; ^ Matomo convention: the file has a .php extension, so if it is ever requested over HTTP, PHP
;   parses it and exits here instead of dumping the settings. Everything below is plain INI.
;
; DEV overrides for Matomo's global.ini.php. Matomo loads
;   global.ini.php  ->  common.config.ini.php (this file)  ->  config.ini.php
; and the install wizard + admin UI only ever write config.ini.php — so what is set here survives
; the installer and every version upgrade.
;
; MIRROR of common.config.prod.ini.php — change both together. The only intended differences are
; the http/https block, archiving, and the two dev-only conveniences at the bottom.

[General]
; Matomo is reachable two ways in dev:
;   http://localhost:8080/matomo/   through nginx — same shape as prod; INSTALL THROUGH THIS ONE
;   http://localhost:8084/          the published container port, for quick poking
; proxy_uri_header only takes effect when the X-Forwarded-Uri header is actually present, so the
; direct port keeps working unchanged.
;
; nginx STRIPS the /matomo/ prefix (proxy_pass http://$upstream_matomo/; — note the trailing
; slash) and hands it back via `proxy_set_header X-Forwarded-Uri /matomo`. proxy_uri_header makes
; Matomo prepend that value when it builds its own script name. Drop either half and every
; absolute URL and login redirect loses the /matomo prefix.
proxy_uri_header = 1
proxy_client_headers[] = HTTP_X_FORWARDED_FOR
proxy_host_headers[] = HTTP_X_FORWARDED_HOST
; Exactly one trusted proxy (nginx). $proxy_add_x_forwarded_for appends the real peer address to
; whatever the client sent, so the LAST entry is the only one a visitor cannot forge by hand.
proxy_ip_read_last_in_list = 1

; Dev is plain http. The prod file sets both of these to 1.
assume_secure_protocol = 0
force_ssl = 0

; No archiving sidecar in dev — let the browser trigger archiving so data shows up immediately.
enable_browser_archiving_triggering = 1

; DEV ONLY. Matomo is reachable as localhost:8080 and localhost:8084; rather than keeping several
; trusted_hosts[] entries in sync, skip the check. Prod relies on it and must not set this.
enable_trusted_host_check = 0

; DEV ONLY. `ng serve` runs on :4200 and therefore tracks cross-origin. Most tracker hits are GET
; image beacons (no preflight), but Matomo switches to POST once the URL gets long.
cors_domains[] = "http://localhost:4200"
