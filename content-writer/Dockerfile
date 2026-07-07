# Content Writer API — Railway / Docker
# Build from Geek-SEO repo root:
#   docker build -f content-writer/Dockerfile -t content-writer-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY content-writer/backend/ content-writer/backend/
WORKDIR /src/content-writer/backend
RUN dotnet publish src/ContentWriter.Api/ContentWriter.Api.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-5199}
EXPOSE 5199
ENTRYPOINT ["dotnet", "ContentWriter.Api.dll"]
