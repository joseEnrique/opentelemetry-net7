# Build stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy csproj and restore dependencies
COPY ServiceB.csproj .
RUN dotnet restore -a $TARGETARCH

# Copy source code and build
COPY . .
RUN dotnet publish -c Release -a $TARGETARCH -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 80
ENTRYPOINT ["dotnet", "ServiceB.dll"] 