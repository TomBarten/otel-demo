FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NumberService.ConsoleApp/NumberService.ConsoleApp.csproj", "NumberService.ConsoleApp/"]
RUN dotnet restore "NumberService.ConsoleApp/NumberService.ConsoleApp.csproj"
COPY . .
WORKDIR "/src/NumberService.ConsoleApp"
RUN dotnet build "NumberService.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NumberService.ConsoleApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NumberService.ConsoleApp.dll"]
