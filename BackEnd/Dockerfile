FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Pc.Dominio/Pc.Dominio.csproj Pc.Dominio/
COPY Pc.Infraestrutura/Pc.Infraestrutura.csproj Pc.Infraestrutura/
COPY Pc.Repositorio/Pc.Repositorio.csproj Pc.Repositorio/
COPY Pc.Servico/Pc.Servico.csproj Pc.Servico/
COPY Pc.WebApi/Pc.WebApi.csproj Pc.WebApi/

RUN dotnet restore Pc.WebApi/Pc.WebApi.csproj

COPY . .
RUN dotnet publish Pc.WebApi/Pc.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh \
    && apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

# Healthcheck consulta o endpoint /api/health (verifica app + conexão com o banco).
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- "http://localhost:${PORT:-8080}/api/health" || exit 1

ENTRYPOINT ["/app/entrypoint.sh"]
