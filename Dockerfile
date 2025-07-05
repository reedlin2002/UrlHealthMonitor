# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 把 csproj 複製進來
COPY UrlHealthMonitorApp/*.csproj ./UrlHealthMonitorApp/
RUN dotnet restore UrlHealthMonitorApp/UrlHealthMonitorApp.csproj

# 把所有原始碼複製進來
COPY UrlHealthMonitorApp/. ./UrlHealthMonitorApp/
WORKDIR /src/UrlHealthMonitorApp
RUN dotnet publish -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
# 建立資料目錄
RUN mkdir /app/data
ENV DATABASE_PATH=/app/data/results.db

# 開放 Port 5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "UrlHealthMonitorApp.dll", "serve"]
