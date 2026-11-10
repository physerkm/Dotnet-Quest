# Render (veya Docker çalıştıran herhangi bir platform) bu dosyayla siteyi derler ve çalıştırır.

# 1) React arayüzünü derle. Çıktı vite.config.ts'teki ayara göre src/DotnetQuest.Api/wwwroot klasörüne gider.
FROM node:22-slim AS client
WORKDIR /src/client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ ./
RUN npm run build

# 2) API'yi derle ve yayın dosyalarını çıkar. React yukarıda derlendiği için npm adımları atlanır.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/DotnetQuest.Api/DotnetQuest.Api.csproj src/DotnetQuest.Api/
RUN dotnet restore src/DotnetQuest.Api/DotnetQuest.Api.csproj
COPY src/DotnetQuest.Api/ src/DotnetQuest.Api/
COPY --from=client /src/src/DotnetQuest.Api/wwwroot src/DotnetQuest.Api/wwwroot
RUN dotnet publish src/DotnetQuest.Api/DotnetQuest.Api.csproj -c Release -o /app --no-restore -p:SkipClientBuild=true

# 3) Sadece çalışma zamanını içeren küçük imaj (SDK ve Node burada yok).
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID

# Render dinlenecek portu PORT ortam değişkeniyle bildirir; tanımlı değilse 8080 kullanılır.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet DotnetQuest.Api.dll"]
