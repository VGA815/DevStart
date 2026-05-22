# DevStart backend — build, test & container automation.
# Override any variable on the command line, e.g.: make build CONFIG=Debug

SOLUTION ?= DevStart.slnx
WEBAPI   ?= src/DevStart.WebApi/DevStart.WebApi.csproj
INFRA    ?= src/DevStart.Infrastructure
CONFIG   ?= Release
IMAGE    ?= devstartwebapi
COMPOSE_PROD ?= docker-compose.prod.yml

.DEFAULT_GOAL := help
.PHONY: help restore build rebuild test run publish secrets migrate \
        docker-build up down logs up-prod down-prod logs-prod clean

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-14s\033[0m %s\n", $$1, $$2}'

restore: ## Restore NuGet dependencies
	dotnet restore $(SOLUTION)

build: ## Build the solution (Release by default)
	dotnet build $(SOLUTION) -c $(CONFIG)

rebuild: ## Clean + build from scratch
	dotnet build $(SOLUTION) -c $(CONFIG) --no-incremental

test: ## Run all unit/architecture tests
	dotnet test $(SOLUTION) -c $(CONFIG)

run: ## Run the WebApi locally (dotnet run)
	dotnet run --project $(WEBAPI)

publish: ## Publish the WebApi to ./publish
	dotnet publish $(WEBAPI) -c $(CONFIG) -o ./publish

secrets: ## Generate strong .env secrets to stdout (see gen-secrets.sh)
	sh gen-secrets.sh

migrate: ## Apply EF Core migrations (needs `dotnet tool install -g dotnet-ef`)
	dotnet ef database update --project $(INFRA) --startup-project src/DevStart.WebApi

docker-build: ## Build the WebApi container image
	docker build -f src/DevStart.WebApi/Dockerfile -t $(IMAGE):latest .

up: ## Dev stack up (docker-compose.yml + override) with rebuild
	docker compose up -d --build

down: ## Dev stack down
	docker compose down

logs: ## Follow dev stack logs
	docker compose logs -f

up-prod: ## Prod stack up (docker-compose.prod.yml) with rebuild — needs a filled .env
	docker compose -f $(COMPOSE_PROD) up -d --build

down-prod: ## Prod stack down
	docker compose -f $(COMPOSE_PROD) down

logs-prod: ## Follow prod stack logs
	docker compose -f $(COMPOSE_PROD) logs -f

clean: ## Remove build output (bin/obj + ./publish)
	dotnet clean $(SOLUTION) -c $(CONFIG)
	rm -rf publish
