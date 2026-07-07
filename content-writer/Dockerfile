# Content Writer API — local Docker only (production uses GeekSeoBackend).
# Build context: Geek-SEO repo root (docker build -f content-writer/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY content-writer/backend/ backend/
WORKDIR /src/backend
RUN dotnet publish src/ContentWriter.Api/ContentWriter.Api.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5199
ENTRYPOINT ["dotnet", "ContentWriter.Api.dll"]
