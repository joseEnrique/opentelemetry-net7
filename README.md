# Microservicios .NET 7 con OpenTelemetry y MongoDB

Este proyecto contiene un escenario completo de microservicios en .NET 7 con instrumentación OpenTelemetry y base de datos MongoDB.

## Estructura del Proyecto

```
├── ServiceA/                 # Servicio A - API simple
│   ├── Program.cs
│   ├── ServiceA.csproj
│   └── Dockerfile
├── ServiceB/                 # Servicio B - API con MongoDB
│   ├── Models/
│   │   └── Item.cs
│   ├── Program.cs
│   ├── ServiceB.csproj
│   └── Dockerfile
├── k8s/                     # Manifiestos de Kubernetes
│   ├── namespace.yaml
│   ├── mongodb.yaml
│   ├── servicea.yaml
│   ├── serviceb.yaml
│   └── ingress.yaml
└── helms/                   # Configuración OpenTelemetry
    └── otel.yaml
```

## Servicios

### ServiceA
- **Endpoints**:
  - `GET /api/values` - Devuelve una lista estática de strings
  - `GET /api/items-from-b` - Consulta ServiceB para obtener items de MongoDB
- **Health Check**: `GET /health`
- **Puerto**: 80

### ServiceB  
- **Endpoints**:
  - `GET /api/values-from-a` - Consulta ServiceA vía HTTP
  - `GET /api/items` - Lee documentos de MongoDB colección "items"
- **Health Check**: `GET /health`
- **Puerto**: 80

## Construcción de Imágenes Docker

### ⚠️ IMPORTANTE: Compatibilidad de Arquitectura

Los Dockerfiles están optimizados para **multi-plataforma**. Usa siempre `docker buildx` para evitar errores de "exec format error":

```bash
# Verificar arquitectura de tu cluster Kubernetes
kubectl get nodes -o wide

# Construcción para arquitectura específica (recomendado)
# Para clusters AMD64 (Intel/AMD - más común en cloud)
docker buildx build --platform linux/amd64 -t servicea:latest ServiceA/ --load
docker buildx build --platform linux/amd64 -t serviceb:latest ServiceB/ --load

# Para clusters ARM64 (Apple Silicon, algunos clusters)
docker buildx build --platform linux/arm64 -t servicea:latest ServiceA/ --load
docker buildx build --platform linux/arm64 -t serviceb:latest ServiceB/ --load

# Multi-arquitectura (compatible con cualquier cluster)
docker buildx build --platform linux/amd64,linux/arm64 -t servicea:latest ServiceA/ --push
docker buildx build --platform linux/amd64,linux/arm64 -t serviceb:latest ServiceB/ --push
```

### 🎯 Usando Makefiles (Recomendado)

Cada servicio incluye un **Makefile** optimizado con todos los comandos necesarios:

```bash
# ServiceA
cd ServiceA
make help                    # Ver todos los comandos disponibles
make dev                     # Desarrollo con hot reload en puerto 5000
make docker-buildx           # Construir imagen multi-plataforma
make test-api               # Probar todos los endpoints

# ServiceB  
cd ServiceB
make help                    # Ver todos los comandos disponibles
make dev                     # Desarrollo con hot reload en puerto 5001
make docker-buildx           # Construir imagen multi-plataforma
make test-full-stack        # Probar stack completo (requiere MongoDB y ServiceA)
```

### Construcción Tradicional (solo si buildx no está disponible)
```bash
# Solo para desarrollo local
cd ServiceA && docker build -t servicea:latest .
cd ../ServiceB && docker build -t serviceb:latest .
```

## 🔧 Solución a "exec format error"

Si obtienes este error en Kubernetes, significa incompatibilidad de arquitectura:

1. **Verificar arquitectura del cluster**:
```bash
kubectl describe node | grep "kubernetes.io/arch"
```

2. **Reconstruir con la arquitectura correcta**:
```bash
# Eliminar imágenes actuales
docker rmi servicea:latest serviceb:latest

# Reconstruir para la arquitectura de tu cluster
docker buildx build --platform linux/amd64 -t servicea:latest ServiceA/ --load
docker buildx build --platform linux/amd64 -t serviceb:latest ServiceB/ --load
```

3. **Re-desplegar**:
```bash
kubectl delete -f k8s/servicea.yaml k8s/serviceb.yaml
kubectl apply -f k8s/servicea.yaml k8s/serviceb.yaml
```

## Despliegue en Kubernetes

```bash
# Crear namespace
kubectl apply -f k8s/namespace.yaml

# Desplegar MongoDB
kubectl apply -f k8s/mongodb.yaml

# Desplegar ServiceA
kubectl apply -f k8s/servicea.yaml

# Desplegar ServiceB
kubectl apply -f k8s/serviceb.yaml

# (Opcional) Desplegar Ingress
kubectl apply -f k8s/ingress.yaml
```

## Variables de Entorno

### ServiceA
- `OTEL_SERVICE_NAME`: Nombre del servicio para OpenTelemetry (default: "ServiceA")
- `OTEL_EXPORTER_JAEGER_ENDPOINT`: Endpoint de Jaeger (default: "http://jaeger-collector.observability.svc.cluster.local:14268/api/traces")
- `ALLOWED_ORIGINS`: Orígenes permitidos para CORS (default: "*")
- `SERVICEB_URL`: URL de ServiceB (default: "http://localhost:5001")

### ServiceB
- `OTEL_SERVICE_NAME`: Nombre del servicio para OpenTelemetry (default: "ServiceB")
- `OTEL_EXPORTER_JAEGER_ENDPOINT`: Endpoint de Jaeger (default: "http://jaeger-collector.observability.svc.cluster.local:14268/api/traces")
- `ALLOWED_ORIGINS`: Orígenes permitidos para CORS (default: "*")
- `SERVICEA_URL`: URL de ServiceA (default: "http://localhost:5000")
- `MONGO_CONNECTION`: Cadena de conexión a MongoDB (default: "mongodb://localhost:27017")
- `MONGO_DATABASE`: Nombre de la base de datos MongoDB (default: "testdb")

## 🚀 Flujo de Desarrollo Rápido

### Desarrollo Local Completo
```bash
# 1. Iniciar MongoDB (Docker)
docker run -d --name mongo -p 27017:27017 mongo:6.0

# 2. Terminal 1: ServiceA
cd ServiceA
make dev              # Hot reload en http://localhost:5000

# 3. Terminal 2: ServiceB  
cd ServiceB
make dev              # Hot reload en http://localhost:5001

# 4. Probar el stack completo
cd ServiceB
make test-full-stack  # Prueba todos los endpoints e interacciones
```

### Construcción y Despliegue
```bash
# Construir imágenes multi-plataforma (usa quixpublic.azurecr.io por defecto)
cd ServiceA && make docker-buildx
cd ../ServiceB && make docker-buildx

# O con registry personalizado
DOCKER_REGISTRY=your-registry.com DOCKER_TAG=v1.0.0 make docker-buildx

# Las imágenes se subirán automáticamente a:
# - quixpublic.azurecr.io/servicea:latest
# - quixpublic.azurecr.io/serviceb:latest
```

## Características

✅ **OpenTelemetry**: Instrumentación completa con exportación a Jaeger  
✅ **CORS**: Configurado para permitir orígenes específicos  
✅ **Health Checks**: Endpoints `/health` para readiness y liveness probes  
✅ **MongoDB**: Integración con MongoDB.Driver  
✅ **Docker**: Imágenes multi-stage optimizadas con soporte **multi-plataforma**  
✅ **Kubernetes**: Deployments con 3 réplicas, Services y Ingress  
✅ **Minimal APIs**: Implementación moderna con .NET 7  
✅ **Makefiles**: Automatización completa de desarrollo y despliegue  

## Pruebas Locales

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

## Pruebas en Kubernetes

```bash
# Port-forward para acceso local
kubectl port-forward -n microservices-demo svc/servicea 8080:80
kubectl port-forward -n microservices-demo svc/serviceb 8081:80

# Probar endpoints
curl http://localhost:8080/api/values
curl http://localhost:8080/api/items-from-b
curl http://localhost:8081/api/values-from-a
curl http://localhost:8081/api/items
```

## Acceso vía Ingress

Si se despliega el Ingress, agregar al `/etc/hosts`:
```
127.0.0.1 microservices.local
```

Luego acceder a:
- ServiceA: `http://microservices.local/a/api/values`
- ServiceA: `http://microservices.local/a/api/items-from-b`
- ServiceB: `http://microservices.local/b/api/values-from-a`
- ServiceB: `http://microservices.local/b/api/items`

## Arquitectura de Comunicación

El escenario ahora implementa **comunicación bidireccional** entre los servicios:

- **ServiceA → ServiceB**: El endpoint `/api/items-from-b` de ServiceA consulta `/api/items` de ServiceB para obtener datos de MongoDB
- **ServiceB → ServiceA**: El endpoint `/api/values-from-a` de ServiceB consulta `/api/values` de ServiceA para obtener datos estáticos

Ambos servicios mantienen sus propias responsabilidades:
- **ServiceA**: Proporciona datos estáticos y actúa como cliente de ServiceB
- **ServiceB**: Maneja la persistencia en MongoDB y actúa como cliente de ServiceA

Esta arquitectura permite patrones de comunicación más complejos y realistas en un entorno de microservicios, donde los servicios pueden actuar tanto como proveedores como consumidores de APIs.

## Acceso a Jaeger UI

Para acceder a la interfaz de Jaeger y visualizar las trazas:

```bash
# Port-forward para acceder a Jaeger UI desde localhost
kubectl port-forward -n observability svc/jaeger-query 16686:16686

# Abrir en el navegador
open http://localhost:16686
```

En Jaeger UI podrás ver:
- 🔍 **Trazas distribuidas** entre ServiceA ↔ ServiceB
- 📊 **Métricas de latencia** de las llamadas HTTP
- 🌐 **Mapas de servicios** mostrando la comunicación entre microservicios
- 🐛 **Debugging** de requests y errores en tiempo real

## Troubleshooting

### "exec format error"
- **Causa**: Incompatibilidad de arquitectura entre imagen y nodo
- **Solución**: Reconstruir con `docker buildx --platform linux/amd64`

### Imágenes no se actualizan en Kubernetes
- **Configuración**: Los manifiestos usan `imagePullPolicy: Always`
- **Comportamiento**: Kubernetes siempre descarga la imagen más reciente del registry
- **Ventaja**: Los cambios se reflejan inmediatamente sin cambiar tags
- **Para forzar actualización**: `kubectl rollout restart deployment/servicea -n microservices-demo`

### Pods en estado CrashLoopBackOff
- **Verificar logs**: `kubectl logs -n microservices-demo deployment/servicea`
- **Verificar recursos**: `kubectl describe pod -n microservices-demo`

### Jaeger no recibe trazas
- **Verificar conectividad**: Endpoint debe ser `jaeger-collector.observability.svc.cluster.local:14268`
- **Verificar logs de servicios**: Buscar errores de OpenTelemetry

### MongoDB connection failed
- **Verificar que MongoDB esté ejecutándose**: `kubectl get pods -n microservices-demo`
- **Verificar URL**: Debe ser `mongodb://mongo.microservices-demo.svc.cluster.local:27017` 