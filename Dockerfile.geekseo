# GeekSeoBackend — Railway / Docker
# Build context: Geek-SEO repo root (no GeekBackend clone after M2/M7).
#
#   docker build -f Dockerfile -t geek-seo-backend .
#
# Runtime uses dotnet/runtime-deps + playwright.ps1 install instead of
# mcr.microsoft.com/playwright/dotnet — that image hits MCR 429 rate limits
# on Railway builders more often than the standard dotnet images.

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY GeekSeo.Persistence/ GeekSeo.Persistence/
COPY GeekSeo.Application/ GeekSeo.Application/
COPY GeekSeoBackend/ GeekSeoBackend/
WORKDIR /src/GeekSeoBackend
RUN dotnet publish GeekSeoBackend.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble AS runtime
WORKDIR /app

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
COPY --from=build /app/publish .

# Install Chromium + OS deps via the Playwright CLI shipped with the app.
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget ca-certificates \
    && wget -q https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb \
    && dpkg -i /tmp/packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends powershell \
    && pwsh ./playwright.ps1 install --with-deps chromium \
    && apt-get purge -y wget powershell \
    && apt-get autoremove -y \
    && rm -rf /var/lib/apt/lists/* /tmp/*

RUN chmod +x ./GeekSeoBackend
ENV PORT=5051
ENV ASPNETCORE_URLS=http://0.0.0.0:5051
EXPOSE 5051
ENTRYPOINT ["./GeekSeoBackend"]
