FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["services/Product/Ecommerce.Product/Ecommerce.Product.csproj", "services/Product/Ecommerce.Product/"]
COPY ["shared/Ecommerce.BuildingBlocks/Ecommerce.BuildingBlocks.csproj", "shared/Ecommerce.BuildingBlocks/"]
COPY ["shared/Ecommerce.BuildingBlocks.Persistence/Ecommerce.BuildingBlocks.Persistence.csproj", "shared/Ecommerce.BuildingBlocks.Persistence/"]
RUN dotnet restore "services/Product/Ecommerce.Product/Ecommerce.Product.csproj"
COPY . .
WORKDIR "/src/services/Product/Ecommerce.Product"
RUN dotnet build "./Ecommerce.Product.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ecommerce.Product.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.Product.dll"]
