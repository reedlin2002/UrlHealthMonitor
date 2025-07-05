# Build階段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "./UrlHealthMonitorApp.csproj" -c Release -o /app/publish

# Runtime階段
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# 設定環境變數，指定 ASP.NET Core 監聽的 URL
ENV ASPNETCORE_URLS=http://+:5000

EXPOSE 5000

ENTRYPOINT ["dotnet", "UrlHealthMonitorApp.dll", "serve"]
