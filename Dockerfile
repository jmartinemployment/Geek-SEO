# GeekSeoBackend — Railway / Docker
#
# Local: GeekBackend is a sibling repo (../GeekBackend) — same relative path as below.
# Railway: Dockerfile clones GeekBackend before build. Pin commit in GeekBackend.commit.
#
# Railway: root directory /, Dockerfile Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
RUN apt-get update && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/*
COPY GeekBackend.commit .
RUN set -eu; \
    REF="$(tr -d '[:space:]' < GeekBackend.commit)"; \
    git clone --filter=blob:none --no-checkout https://github.com/jmartinemployment/GeekBackend.git GeekBackend; \
    cd GeekBackend; \
    git fetch --depth 1 origin "${REF}"; \
    git checkout FETCH_HEAD; \
    test -f GeekApplication/GeekApplication.csproj
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
