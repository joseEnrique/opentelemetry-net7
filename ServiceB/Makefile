.PHONY: build clean restore test docker-build docker-push docker-buildx run dev help

# Variables
PROJECT_NAME=ServiceB
DOCKER_REGISTRY ?= quixpublic.azurecr.io
DOCKER_REPO ?= serviceb
DOCKER_TAG ?= new
DOCKER_IMAGE = $(DOCKER_REGISTRY)/$(DOCKER_REPO):$(DOCKER_TAG)
DOCKER_PLATFORMS ?= linux/amd64
TARGETARCH ?= amd64

# .NET Configuration
DOTNET_CONFIG ?= Release
PUBLISH_DIR = ./bin/$(DOTNET_CONFIG)/net7.0/publish

# Default target
help:
	@echo "Available commands for ServiceB (.NET 7 Microservice):"
	@echo ""
	@echo "Development commands:"
	@echo "  restore       - Restore NuGet packages"
	@echo "  build         - Build the .NET project"
	@echo "  run           - Run ServiceB locally"
	@echo "  dev           - Run ServiceB in development mode with hot reload"
	@echo "  test          - Run unit tests"
	@echo "  clean         - Clean build artifacts"
	@echo ""
	@echo "Docker commands:"
	@echo "  docker-build  - Build Docker image (single platform)"
	@echo "  docker-buildx - Build multi-platform Docker image (ARM64 + AMD64)"
	@echo "  docker-push   - Push Docker image to registry"
	@echo "  docker-all    - Build and push multi-platform Docker image"
	@echo ""
	@echo "Usage examples:"
	@echo "  make run                                # Run locally on port 5001"
	@echo "  make dev                                # Run with hot reload"
	@echo "  DOCKER_TAG=v1.0.0 make docker-buildx   # Build with specific tag"
	@echo "  TARGETARCH=amd64 make docker-buildx     # Build for specific architecture"
	@echo "  make docker-all                         # Build and push multi-platform"
	@echo ""
	@echo "Variables:"
	@echo "  DOCKER_REGISTRY=$(DOCKER_REGISTRY)"
	@echo "  DOCKER_REPO=$(DOCKER_REPO)"
	@echo "  DOCKER_TAG=$(DOCKER_TAG)"
	@echo "  DOCKER_PLATFORMS=$(DOCKER_PLATFORMS)"
	@echo "  TARGETARCH=$(TARGETARCH)"
	@echo "  DOTNET_CONFIG=$(DOTNET_CONFIG)"
	@echo ""
	@echo "Endpoints after running:"
	@echo "  GET http://localhost:5001/api/values-from-a - Values from ServiceA"
	@echo "  GET http://localhost:5001/api/items         - Items from MongoDB"
	@echo "  GET http://localhost:5001/health            - Health check"
	@echo ""
	@echo "Dependencies:"
	@echo "  - MongoDB running on mongodb://localhost:27017"
	@echo "  - ServiceA running on http://localhost:5000 (for values-from-a endpoint)"

# Restore NuGet packages
restore:
	@echo "Restoring NuGet packages for $(PROJECT_NAME)..."
	dotnet restore $(PROJECT_NAME).csproj
	@echo "Packages restored successfully"

# Build the project
build: restore
	@echo "Building $(PROJECT_NAME) in $(DOTNET_CONFIG) mode..."
	dotnet build $(PROJECT_NAME).csproj -c $(DOTNET_CONFIG) --no-restore
	@echo "Build completed successfully"

# Publish the project
publish: restore
	@echo "Publishing $(PROJECT_NAME)..."
	dotnet publish $(PROJECT_NAME).csproj -c $(DOTNET_CONFIG) -o $(PUBLISH_DIR) --no-restore
	@echo "Published to: $(PUBLISH_DIR)"

# Run the application locally
run: build
	@echo "Starting $(PROJECT_NAME) on http://localhost:5001..."
	@echo "Make sure MongoDB is running on mongodb://localhost:27017"
	@echo "Make sure ServiceA is running on http://localhost:5000"
	@echo "Press Ctrl+C to stop"
	ASPNETCORE_URLS=http://localhost:5001 dotnet run --project $(PROJECT_NAME).csproj

# Run in development mode with hot reload
dev:
	@echo "Starting $(PROJECT_NAME) in development mode with hot reload..."
	@echo "Available on http://localhost:5001"
	@echo "Press Ctrl+C to stop"
	ASPNETCORE_URLS=http://localhost:5001 dotnet watch run --project $(PROJECT_NAME).csproj

# Run tests
test:
	@echo "Running tests for $(PROJECT_NAME)..."
	dotnet test $(PROJECT_NAME).csproj --no-build --verbosity normal

# Clean build artifacts
clean:
	@echo "Cleaning build artifacts..."
	dotnet clean $(PROJECT_NAME).csproj
	rm -rf bin/ obj/
	docker rmi $(DOCKER_IMAGE) 2>/dev/null || true
	@echo "Clean completed"

# Docker commands
docker-build:
	@echo "Building Docker image: $(DOCKER_IMAGE)"
	@echo "Target Architecture: $(TARGETARCH)"
	docker build \
		--build-arg TARGETARCH=$(TARGETARCH) \
		-t $(DOCKER_IMAGE) \
		.
	@echo "Docker image built successfully"
	@echo "Run with: docker run -p 5001:80 $(DOCKER_IMAGE)"

docker-buildx-setup:
	@echo "Setting up Docker Buildx..."
	@docker buildx inspect serviceb-builder >/dev/null 2>&1 || \
		docker buildx create --name serviceb-builder --use
	@docker buildx inspect --bootstrap
	@echo "Docker Buildx setup completed"

docker-buildx: docker-buildx-setup
	@echo "Building multi-platform Docker image: $(DOCKER_IMAGE)"
	@echo "Platforms: $(DOCKER_PLATFORMS)"
	@echo "Target Architecture: $(TARGETARCH)"
	docker buildx build \
		--platform $(DOCKER_PLATFORMS) \
		--build-arg TARGETARCH=$(TARGETARCH) \
		--tag $(DOCKER_IMAGE) \
		--push \
		.
	@echo "Multi-platform Docker image built and pushed successfully"

docker-push: docker-build
	@echo "Pushing Docker image: $(DOCKER_IMAGE)"
	docker push $(DOCKER_IMAGE)
	@echo "Docker image pushed successfully"

docker-all: docker-buildx

# Utility commands
logs:
	@echo "Showing logs for ServiceB container..."
	docker logs -f $(PROJECT_NAME) 2>/dev/null || echo "Container not running"

shell:
	@echo "Opening shell in ServiceB container..."
	docker exec -it $(PROJECT_NAME) /bin/bash 2>/dev/null || echo "Container not running"

# Health check
health:
	@echo "Checking ServiceB health..."
	@curl -s http://localhost:5001/health | jq . 2>/dev/null || \
		curl -s http://localhost:5001/health || \
		echo "ServiceB not responding on http://localhost:5001/health"

# API test commands
test-api:
	@echo "Testing ServiceB APIs..."
	@echo "1. Testing /api/values-from-a (requires ServiceA):"
	@curl -s http://localhost:5001/api/values-from-a | jq . 2>/dev/null || curl -s http://localhost:5001/api/values-from-a
	@echo ""
	@echo "2. Testing /api/items (requires MongoDB):"
	@curl -s http://localhost:5001/api/items | jq . 2>/dev/null || curl -s http://localhost:5001/api/items
	@echo ""
	@echo "3. Testing health:"
	@curl -s http://localhost:5001/health | jq . 2>/dev/null || curl -s http://localhost:5001/health

# MongoDB utilities
mongo-status:
	@echo "Checking MongoDB status..."
	@docker ps | grep mongo || echo "MongoDB container not running"
	@echo "Testing MongoDB connection..."
	@timeout 5 bash -c "</dev/tcp/localhost/27017" 2>/dev/null && echo "MongoDB is accessible on port 27017" || echo "MongoDB not accessible on port 27017"

# Combined development workflow
dev-all: clean restore build test run

# Production deployment
deploy: docker-all
	@echo "ServiceB deployed successfully"
	@echo "Update Kubernetes manifests with image: $(DOCKER_IMAGE)"

# Full stack test (requires both services and MongoDB)
test-full-stack: mongo-status
	@echo "Testing full microservices stack..."
	@echo "1. ServiceB health:"
	@make health
	@echo ""
	@echo "2. ServiceB → MongoDB:"
	@curl -s http://localhost:5001/api/items | jq . 2>/dev/null || curl -s http://localhost:5001/api/items
	@echo ""
	@echo "3. ServiceB → ServiceA:"
	@curl -s http://localhost:5001/api/values-from-a | jq . 2>/dev/null || curl -s http://localhost:5001/api/values-from-a 