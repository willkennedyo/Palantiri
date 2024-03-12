#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Palantiri.Worker.Registry/Palantiri.Worker.Registry.csproj", "Palantiri.Worker.Registry/"]
COPY ["Palantiri.Shared/Palantiri.Shared.csproj", "Palantiri.Shared/"]
RUN dotnet restore "./Palantiri.Worker.Registry/Palantiri.Worker.Registry.csproj"
COPY . .
WORKDIR "/src/Palantiri.Worker.Registry"
RUN dotnet build "./Palantiri.Worker.Registry.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Palantiri.Worker.Registry.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Palantiri.Worker.Registry.dll"]