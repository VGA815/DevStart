# DevStart — Backend

![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1)
![License](https://img.shields.io/badge/license-AGPL--3.0-blue)

**DevStart** is a platform that helps early-stage startups raise investment and assemble their teams. This repository is the backend — a containerized REST API and the supporting services that run it.

## Features

- **Startups** — profiles with members, products, metrics, roadmap, competitors, followers, and documents, plus a computed startup score
- **Investors** — investor profiles, investment applications, deals, and generated deal documents (term sheets, cap tables)
- **Experts** — expert profiles, experience, specializations, and collaboration requests
- **Profiles** — one personal profile per user (name, bio, avatar, links, visibility); the startup-member, expert, and investor roles reference it and add only role-specific data, so personal details live in a single place
- **Subscriptions & payments** — Pro plan billed via YooKassa (one-time redirect payment + НПД receipts)
- **Auth** — JWT bearer + 30-day refresh tokens, OAuth (Google, GitHub), email verification, invite tokens. Legal consents (public offer, personal-data processing, cookies, privacy policy, terms) are captured on **every** sign-up — including OAuth, via a two-step completion that does not create the account until consent is given — and re-prompted when a document version changes. Password and OAuth sign-in both return either tokens or a consent challenge (completed at `POST api/auth/oauth/complete`)
- **Messaging & notifications** — direct messages and real-time notifications over Centrifugo (WebSocket)
- **Media** — avatars and documents stored in MinIO (S3-compatible), served via presigned URLs

## Tech stack

| Area | Technology |
| --- | --- |
| Runtime | .NET 10, ASP.NET Core Minimal APIs |
| Database | PostgreSQL (EF Core + Npgsql) |
| Cache | Redis |
| Object storage | MinIO (S3-compatible) |
| Real-time | Centrifugo (WebSocket) |
| Background jobs | Hangfire |
| Payments | YooKassa |
| Email | SMTP (Mailpit in dev) |
| Logging | Serilog → Seq |
| Reverse proxy | nginx (+ Let's Encrypt / certbot in prod) |
| API docs | Swagger / OpenAPI |

## Prerequisites

- **Docker & Docker Compose** — the only requirement to run the full stack
- **.NET 10 SDK** — optional; only needed to build/test on the host or create EF Core migrations

## Quick start (local dev)

```bash
git clone https://github.com/VGA815/DevStart.git
cd DevStart

cp .env.example .env      # dev defaults work as-is
make up                   # build & start the full stack (or: docker compose up -d --build)
```

Database migrations are applied automatically on startup. Once the stack is healthy:

- **API** — http://localhost:5000
- **Swagger UI** — http://localhost:5000/swagger
- **Through nginx** (mirrors prod routing) — http://localhost:8080

Use `make down` to stop and `make logs` to follow logs.

## Common commands

| Command | Description |
| --- | --- |
| `make up` / `make down` | Start / stop the dev stack (Docker) |
| `make logs` | Follow dev stack logs |
| `make build` | Build the solution (Release) |
| `make test` | Run unit + architecture tests |
| `make run` | Run the WebApi on the host (`dotnet run`) |
| `make migrate` | Apply EF Core migrations manually |
| `make secrets` | Generate strong secrets for a prod `.env` |
| `make up-prod` / `make down-prod` | Start / stop the production stack |

Run `make help` to list every target.

## Services & ports (dev)

| Service | URL / port | Notes |
| --- | --- | --- |
| API (HTTP / HTTPS) | 5000 / 5001 | `health` at `/health`; `/swagger` and `/hangfire` are **Development only** |
| nginx | http://localhost:8080 | Fronts the API, object storage, and the WebSocket |
| PostgreSQL | localhost:5432 | `postgres` / `postgres` |
| Redis | localhost:6379 | |
| MinIO | http://localhost:9001 | Console — `minioadmin` / `minioadminpassword` (S3 API on 9000) |
| Seq | http://localhost:8081 | Structured logs |
| Centrifugo | localhost:8082 | Real-time API / WebSocket |
| Mailpit | http://localhost:8025 | Captures dev email (SMTP on 1025) |

## Configuration

Local development settings ship in `src/DevStart.WebApi/appsettings.Development.json` and work out of the box against the Docker services. OAuth, SMTP, and YooKassa credentials are left blank — fill them only if you need those flows (via `dotnet user-secrets` or the file).

In production, **all** configuration comes from `.env`, consumed by `docker-compose.prod.yml`. See [`.env.example`](.env.example) for the full list of variables.

## Production deployment

The production stack (`docker-compose.prod.yml`) runs the API, PostgreSQL, Redis, MinIO, Centrifugo, Seq, the frontend SPA, and nginx with automatic TLS via Let's Encrypt. nginx terminates TLS on 80/443 and routes:

- `/api/` → API · `/health` → API health check
- `/connection/websocket` → Centrifugo · `/files/`, `/avatars/`, `/startup-documents/`, `/deal-documents/` → MinIO
- `/` → frontend SPA · `/hangfire` → blocked (404)

```bash
cp .env.example .env
sh gen-secrets.sh >> .env     # generate strong secrets, then fill in the non-secret values
sh init-letsencrypt.sh        # issue the initial TLS certificate for $DOMAIN
make up-prod                  # docker compose -f docker-compose.prod.yml up -d --build
```

Before running `init-letsencrypt.sh`:

- Set `DOMAIN` and `LETSENCRYPT_EMAIL` in `.env` — the nginx config is rendered from `config/nginx/nginx.prod.conf.template` with these values at container start.
- Point DNS for `DOMAIN` at the host (ports 80/443 reachable from the internet).
- The `frontend` service builds from a sibling `../devstart-client` repository — clone it alongside this one, or remove the service if the SPA is deployed separately.

## Testing

```bash
dotnet test DevStart.slnx                     # all tests
dotnet test tests/DevStart.UnitTests          # unit tests only
dotnet test tests/DevStart.ArchitectureTests  # layer-boundary checks
```

Run the architecture tests after moving types between projects — a failure means a broken dependency direction.

## Architecture

Clean Architecture with CQRS across four layers (`WebApi → Application → Domain`, with `Infrastructure` and a shared `SharedKernel`). Handlers return a `Result` / `Result<T>` railway type, and endpoints self-register via `IEndpoint`.

The startup scoring engine is documented in **[docs/scoring-methodology.md](docs/scoring-methodology.md)**, and the valuation engine with its data sourcing in **[docs/valuation-methodology.md](docs/valuation-methodology.md)**.

## Documentation

| Doc | For |
| --- | --- |
| **[docs/api.md](docs/api.md)** | REST API reference — conventions (auth, errors, pagination, rate limits) and the full endpoint catalog |
| **[docs/frontend.md](docs/frontend.md)** | Frontend integration guide — auth lifecycle, consent challenge, OAuth, realtime, files, payments |
| **[docs/business-logic.md](docs/business-logic.md)** | Domain & business rules, lifecycles, and the wire-value status reference |
| **[docs/scoring-methodology.md](docs/scoring-methodology.md)** | How the startup score is computed, and why no factor rewards withholding data |
| **[docs/valuation-methodology.md](docs/valuation-methodology.md)** | How startup valuations are computed |
| **[DEPLOYMENT.md](DEPLOYMENT.md)** | Production deployment & operations |

> The authoritative, always-current API contract is the OpenAPI document at `/swagger` (Development).

## Contributing & license

- Contributions — see **[CONTRIBUTING.md](CONTRIBUTING.md)** and the issue templates under [`.github/`](.github/).
- Licensed under the **GNU AGPL-3.0** — see **[LICENSE](LICENSE)**.
