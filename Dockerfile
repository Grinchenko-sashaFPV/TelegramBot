# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ScheduleTelegramBot.csproj", "."]
RUN dotnet restore "./ScheduleTelegramBot.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ScheduleTelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ScheduleTelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Dependencies
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS deps
RUN apt-get update \
 && apt-get install -y --no-install-recommends \
      ca-certificates \
      libgtk-3-0 \
      libgbm1 \
      libnss3 \
      libgconf-2-4 \
      libasound2 \
      libxss1 \
      fonts-liberation \
      xfonts-base \
      xfonts-75dpi \
      wkhtmltopdf \
 && rm -rf /var/lib/apt/lists/*


# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM deps AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ScheduleTelegramBot.dll"]
