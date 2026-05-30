# GeekSeoBackend — Railway / Docker
# Build context: Geek-SEO repo root (no GeekBackend clone after M2/M7).
#
#   docker build -f Dockerfile -t geek-seo-backend .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble
WORKDIR /app
COPY --from=build /app/publish .
RUN chmod +x ./GeekSeoBackend
ENV PORT=5051
ENV ASPNETCORE_URLS=http://0.0.0.0:5051
EXPOSE 5051
ENTRYPOINT ["./GeekSeoBackend"]
