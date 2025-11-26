# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore
COPY ["SmartFoodAPI/SmartFoodAPI/SmartFoodAPI.csproj", "SmartFoodAPI/"]
COPY ["SmartFoodAPI/BLL/BLL.csproj", "BLL/"]
COPY ["SmartFoodAPI/DAL/DAL.csproj", "DAL/"]
RUN dotnet restore "./SmartFoodAPI/SmartFoodAPI.csproj"

# Copy all source code
COPY . .

# Publish directly to /app/publish
WORKDIR "/src/SmartFoodAPI"
RUN dotnet publish "./SmartFoodAPI.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartFoodAPI.dll"]
