# GeekSeoBackend — Railway / Docker
# Build context: Geek-SEO repo root. Clones GeekApplication from GeekBackend (sibling path in csproj).
#
# Railway service settings:
#   Root directory: /
#   Dockerfile: Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
RUN apt-get update && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/*
RUN git clone --depth 1 https://github.com/jmartinemployment/GeekBackend.git GeekBackend
COPY . Geek-SEO/
WORKDIR /src/Geek-SEO/GeekSeoBackend
RUN dotnet publish GeekSeoBackend.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble
WORKDIR /app
COPY --from=build /app/publish .
RUN chmod +x ./GeekSeoBackend
ENV PORT=5051
ENV ASPNETCORE_URLS=http://0.0.0.0:5051
EXPOSE 5051
ENTRYPOINT ["./GeekSeoBackend"]
