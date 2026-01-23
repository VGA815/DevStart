# DevStart Backend

Backend part of **DevStart** — a platform designed to help early-stage startups attract investment and find specialists to build their teams.

This repository contains the server-side infrastructure, APIs, and supporting services required to run the DevStart backend.

## Tech Stack

* **.NET** — core backend platform
* **PostgreSQL** — primary relational database
* **MinIO** — object storage for files and media
* **Seq** — centralized structured logging
* **Swagger (OpenAPI)** — API documentation and testing
* **Docker & Docker Compose** — containerization and local orchestration

## Architecture Overview

* RESTful Web API built with ASP.NET
* Stateless services designed for containerized deployment
* Externalized infrastructure (database, storage, logging)
* OpenAPI specification available via Swagger UI

## Requirements

* Docker
* Docker Compose

No local .NET SDK or database installation is required to run the project.

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/VGA815/DevStart.git
cd DevStart
```

### 2. Run with Docker Compose

```bash
docker-compose up --build
```

This command will start:

* DevStart backend API
* PostgreSQL database
* MinIO object storage
* Seq logging service

## Configuration

All configuration is provided via environment variables defined in `docker-compose.yml`, including:

* Database connection strings
* MinIO access credentials
* Seq logging endpoint
* ASP.NET environment settings

## Logging

The application uses **Seq** for structured logging.
Logs are automatically sent to the Seq container and can be viewed through the web interface.

## File Storage

**MinIO** is used as an S3-compatible object storage for:

* User-uploaded files
* Media assets
* Documents and attachments

Buckets are created automatically on startup if required.

## API Documentation

Swagger UI is enabled by default and provides:

* Interactive API documentation
* Request/response schemas
* Manual endpoint testing

Accessible at `/swagger`.
