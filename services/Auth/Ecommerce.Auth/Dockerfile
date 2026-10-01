FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# curl cho healthcheck. Phải cài TRƯỚC khi đổi sang USER $APP_UID - sau đó không còn quyền.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Thư mục key ring của Data Protection, tạo sẵn và giao cho user chạy app.
# Docker copy quyền sở hữu của thư mục này vào named volume lúc volume còn trống; thiếu bước
# này thì volume thuộc root, container non-root không ghi được, và triệu chứng là trang đăng
# nhập trả 500 vì không sinh nổi antiforgery token.
RUN mkdir -p /keys && chown -R $APP_UID:$APP_UID /keys

USER $APP_UID
WORKDIR /app
EXPOSE 5044

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Central Package Management: restore bắt buộc phải thấy 3 file này, nếu không lỗi NU1008.
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["services/Auth/Ecommerce.Auth/Ecommerce.Auth.csproj", "services/Auth/Ecommerce.Auth/"]
# Copy cả csproj của project shared vì Auth ProjectReference sang chúng (nếu thiếu -> MSB3202).
COPY ["shared/Ecommerce.BuildingBlocks/Ecommerce.BuildingBlocks.csproj", "shared/Ecommerce.BuildingBlocks/"]
COPY ["shared/Ecommerce.BuildingBlocks.Persistence/Ecommerce.BuildingBlocks.Persistence.csproj", "shared/Ecommerce.BuildingBlocks.Persistence/"]
RUN dotnet restore "services/Auth/Ecommerce.Auth/Ecommerce.Auth.csproj"
COPY . .
WORKDIR "/src/services/Auth/Ecommerce.Auth"
RUN dotnet build "./Ecommerce.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ecommerce.Auth.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.Auth.dll"]
