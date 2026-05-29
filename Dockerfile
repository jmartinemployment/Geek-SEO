# GeekSeoBackend — Railway / Docker
# Build context: Geek-SEO repo root (includes GeekBackend git submodule).
#
# Clone this repo with submodules:
#   git clone --recurse-submodules <url>
# Or after clone:
#   git submodule update --init --recursive
#
# Railway service settings:
#   Root directory: /
#   Dockerfile: Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . Geek-SEO/
WORKDIR /src/Geek-SEO/GeekSeoBackend
RUN test -f ../GeekBackend/GeekApplication/GeekApplication.csproj
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
