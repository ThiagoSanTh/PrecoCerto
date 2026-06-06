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
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Pc.WebApi.dll"]
