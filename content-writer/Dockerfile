# Content Writer API — Railway / Docker
# Build context: content-writer/ (set as Railway service root directory)
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY backend/ backend/
WORKDIR /src/backend
RUN dotnet publish src/ContentWriter.Api/ContentWriter.Api.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5199
ENTRYPOINT ["dotnet", "ContentWriter.Api.dll"]
