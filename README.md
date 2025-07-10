# .NET 7 Microservices with OpenTelemetry and MongoDB

This project contains a complete .NET 7 microservices scenario with OpenTelemetry instrumentation and MongoDB database.

## Project Structure

```
├── ServiceA/                 # Service A - Simple API
│   ├── Program.cs
│   ├── ServiceA.csproj
│   └── Dockerfile
├── ServiceB/                 # Service B - API with MongoDB
│   ├── Models/
│   │   └── Item.cs
│   ├── Program.cs
│   ├── ServiceB.csproj
│   └── Dockerfile
├── k8s/                     # Kubernetes Manifests
│   ├── namespace.yaml
│   ├── mongodb.yaml
│   ├── servicea.yaml
│   ├── serviceb.yaml
│   └── ingress.yaml
└── helms/                   # OpenTelemetry Configuration
    └── otel.yaml
```

## Services

### ServiceA
- **Endpoints**:
  - `GET /api/values` - Returns a static list of strings
  - `GET /api/items-from-b` - Queries ServiceB to get items from MongoDB
- **Health Check**: `GET /health`
- **Port**: 80

### ServiceB  
- **Endpoints**:
  - `GET /api/values-from-a` - Queries ServiceA via HTTP
  - `GET /api/items` - Reads documents from MongoDB "items" collection
- **Health Check**: `GET /health`
- **Port**: 80

## Docker Image Building

### ⚠️ IMPORTANT: Architecture Compatibility

The Dockerfiles are optimized for **multi-platform**. Always use `docker buildx` to avoid "exec format error":

```bash
# Check your Kubernetes cluster architecture
kubectl get nodes -o wide

# Build for specific architecture (recommended)
# For AMD64 clusters (Intel/AMD - most common in cloud)
docker buildx build --platform linux/amd64 -t servicea:latest ServiceA/ --load
docker buildx build --platform linux/amd64 -t serviceb:latest ServiceB/ --load

# For ARM64 clusters (Apple Silicon, some clusters)
docker buildx build --platform linux/arm64 -t servicea:latest ServiceA/ --load
docker buildx build --platform linux/arm64 -t serviceb:latest ServiceB/ --load

# Multi-architecture (compatible with any cluster)
docker buildx build --platform linux/amd64,linux/arm64 -t servicea:latest ServiceA/ --push
docker buildx build --platform linux/amd64,linux/arm64 -t serviceb:latest ServiceB/ --push
```

### 🎯 Using Makefiles (Recommended)

Each service includes an optimized **Makefile** with all necessary commands:

```bash
# ServiceA
cd ServiceA
make help                    # See all available commands
make dev                     # Development with hot reload on port 5000
make docker-buildx           # Build multi-platform image
make test-api               # Test all endpoints

# ServiceB  
cd ServiceB
make help                    # See all available commands
make dev                     # Development with hot reload on port 5001
make docker-buildx           # Build multi-platform image
make test-full-stack        # Test complete stack (requires MongoDB and ServiceA)
```

### Traditional Build (only if buildx is not available)
```bash
# For local development only
cd ServiceA && docker build -t servicea:latest .
cd ../ServiceB && docker build -t serviceb:latest .
```

## 🔧 Solving "exec format error"

If you get this error in Kubernetes, it means architecture incompatibility:

1. **Check cluster architecture**:
```bash
kubectl describe node | grep "kubernetes.io/arch"
```

2. **Rebuild with correct architecture**:
```bash
# Remove current images
docker rmi servicea:latest serviceb:latest

# Rebuild for your cluster architecture
docker buildx build --platform linux/amd64 -t servicea:latest ServiceA/ --load
docker buildx build --platform linux/amd64 -t serviceb:latest ServiceB/ --load
```

3. **Re-deploy**:
```bash
kubectl delete -f k8s/servicea.yaml k8s/serviceb.yaml
kubectl apply -f k8s/servicea.yaml k8s/serviceb.yaml
```

## Kubernetes Deployment

```bash
# Create namespace
kubectl apply -f k8s/namespace.yaml

# Deploy MongoDB
kubectl apply -f k8s/mongodb.yaml

# Deploy ServiceA
kubectl apply -f k8s/servicea.yaml

# Deploy ServiceB
kubectl apply -f k8s/serviceb.yaml

# (Optional) Deploy Ingress
kubectl apply -f k8s/ingress.yaml
```

## Environment Variables

### ServiceA
- `OTEL_SERVICE_NAME`: Service name for OpenTelemetry (default: "ServiceA")
- `OTEL_EXPORTER_JAEGER_ENDPOINT`: Jaeger endpoint (default: "http://jaeger-collector.observability.svc.cluster.local:14268/api/traces")
- `ALLOWED_ORIGINS`: Allowed origins for CORS (default: "*")
- `SERVICEB_URL`: ServiceB URL (default: "http://localhost:5001")

### ServiceB
- `OTEL_SERVICE_NAME`: Service name for OpenTelemetry (default: "ServiceB")
- `OTEL_EXPORTER_JAEGER_ENDPOINT`: Jaeger endpoint (default: "http://jaeger-collector.observability.svc.cluster.local:14268/api/traces")
- `ALLOWED_ORIGINS`: Allowed origins for CORS (default: "*")
- `SERVICEA_URL`: ServiceA URL (default: "http://localhost:5000")
- `MONGO_CONNECTION`: MongoDB connection string (default: "mongodb://localhost:27017")
- `MONGO_DATABASE`: MongoDB database name (default: "testdb")

## 🚀 Quick Development Workflow

### Complete Local Development
```bash
# 1. Start MongoDB (Docker)
docker run -d --name mongo -p 27017:27017 mongo:6.0

# 2. Terminal 1: ServiceA
cd ServiceA
make dev              # Hot reload at http://localhost:5000

# 3. Terminal 2: ServiceB  
cd ServiceB
make dev              # Hot reload at http://localhost:5001

# 4. Test the complete stack
cd ServiceB
make test-full-stack  # Test all endpoints and interactions
```

### Build and Deployment
```bash
# Build multi-platform images (uses quixpublic.azurecr.io by default)
cd ServiceA && make docker-buildx
cd ../ServiceB && make docker-buildx

# Or with custom registry
DOCKER_REGISTRY=your-registry.com DOCKER_TAG=v1.0.0 make docker-buildx

# Images will be automatically pushed to:
# - quixpublic.azurecr.io/servicea:latest
# - quixpublic.azurecr.io/serviceb:latest
```

## Features

✅ **OpenTelemetry**: Complete instrumentation with Jaeger export  
✅ **CORS**: Configured to allow specific origins  
✅ **Health Checks**: `/health` endpoints for readiness and liveness probes  
✅ **MongoDB**: Integration with MongoDB.Driver  
✅ **Docker**: Optimized multi-stage images with **multi-platform** support  
✅ **Kubernetes**: Deployments with 3 replicas, Services and Ingress  
✅ **Minimal APIs**: Modern implementation with .NET 7  
✅ **Makefiles**: Complete automation for development and deployment  

## Local Testing

```bash
# ServiceA
curl http://localhost:5000/api/values
curl http://localhost:5000/api/items-from-b
curl http://localhost:5000/health

# ServiceB  
curl http://localhost:5001/api/values-from-a
curl http://localhost:5001/api/items
curl http://localhost:5001/health
```

## Kubernetes Testing

```bash
# Port-forward for local access
kubectl port-forward -n microservices-demo svc/servicea 8080:80
kubectl port-forward -n microservices-demo svc/serviceb 8081:80

# Test endpoints
curl http://localhost:8080/api/values
curl http://localhost:8080/api/items-from-b
curl http://localhost:8081/api/values-from-a
curl http://localhost:8081/api/items
```

## Ingress Access

If Ingress is deployed, add to `/etc/hosts`:
```
127.0.0.1 microservices.local
```

Then access:
- ServiceA: `http://microservices.local/a/api/values`
- ServiceA: `http://microservices.local/a/api/items-from-b`
- ServiceB: `http://microservices.local/b/api/values-from-a`
- ServiceB: `http://microservices.local/b/api/items`

## Communication Architecture

The scenario now implements **bidirectional communication** between services:

- **ServiceA → ServiceB**: ServiceA's `/api/items-from-b` endpoint queries ServiceB's `/api/items` to get MongoDB data
- **ServiceB → ServiceA**: ServiceB's `/api/values-from-a` endpoint queries ServiceA's `/api/values` to get static data

Both services maintain their own responsibilities:
- **ServiceA**: Provides static data and acts as a ServiceB client
- **ServiceB**: Handles MongoDB persistence and acts as a ServiceA client

This architecture enables more complex and realistic communication patterns in a microservices environment, where services can act as both API providers and consumers.

## Jaeger UI Access

To access Jaeger interface and visualize traces:

```bash
# Port-forward to access Jaeger UI from localhost
kubectl port-forward -n observability svc/jaeger-query 16686:16686

# Open in browser
open http://localhost:16686
```

In Jaeger UI you can see:
- 🔍 **Distributed traces** between ServiceA ↔ ServiceB
- 📊 **Latency metrics** from HTTP calls
- 🌐 **Service maps** showing microservices communication
- 🐛 **Real-time debugging** of requests and errors

## Troubleshooting

### "exec format error"
- **Cause**: Architecture incompatibility between image and node
- **Solution**: Rebuild with `docker buildx --platform linux/amd64`

### Images not updating in Kubernetes
- **Configuration**: Manifests use `imagePullPolicy: Always`
- **Behavior**: Kubernetes always downloads the latest image from registry
- **Advantage**: Changes are reflected immediately without changing tags
- **To force update**: `kubectl rollout restart deployment/servicea -n microservices-demo`

### Pods in CrashLoopBackOff state
- **Check logs**: `kubectl logs -n microservices-demo deployment/servicea`
- **Check resources**: `kubectl describe pod -n microservices-demo`

### Jaeger not receiving traces
- **Check connectivity**: Endpoint should be `jaeger-collector.observability.svc.cluster.local:14268`
- **Check service logs**: Look for OpenTelemetry errors

### MongoDB connection failed
- **Check MongoDB is running**: `kubectl get pods -n microservices-demo`
- **Check URL**: Should be `mongodb://mongo.microservices-demo.svc.cluster.local:27017` 