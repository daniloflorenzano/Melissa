FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ARG APP_UID=1000
# Create a user and group so the container doesn't run as root. If a different UID is passed at build time, it will be used.
RUN addgroup --gid ${APP_UID} appgroup \
    && adduser --uid ${APP_UID} --gid ${APP_UID} --disabled-password --gecos "" appuser || true
WORKDIR /app
RUN mkdir -p /app && chown -R appuser:appgroup /app
# Install native dependencies required by Whisper runtime on Linux
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       libc6 \
       libgcc1 \
       libstdc++6 \
       libgomp1 \
    && rm -rf /var/lib/apt/lists/*
USER appuser
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Melissa.WebServer/Melissa.WebServer.csproj", "Melissa.WebServer/"]
COPY ["Melissa.Core/Melissa.Core.csproj", "Melissa.Core/"]
RUN dotnet restore "Melissa.WebServer/Melissa.WebServer.csproj"
COPY . .
WORKDIR "/src/Melissa.WebServer"
RUN dotnet build "./Melissa.WebServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Remove duplicate ggml-metal.metal introduced by multiple whisper runtime packages
# to avoid NETSDK1152 during publish. These files are under the NuGet global-packages
# folder inside the container at /root/.nuget/packages.
RUN rm -f /root/.nuget/packages/whisper.net.runtime.coreml/1.8.1/build/ggml-metal.metal || true \
    && rm -f /root/.nuget/packages/whisper.net.runtime/1.8.1/build/ggml-metal.metal || true

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Melissa.WebServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
WORKDIR /app
COPY --from=publish /app/publish .
# Ensure native libraries have execute permission
RUN find /app -type f -name "*.so" -exec chmod a+x {} \; || true
# Copiar arquivo de feriados para o container
RUN mkdir -p /app/Data
COPY ./Melissa.WebServer/Data/holidays_2025.csv /app/Data/holidays_2025.csv
RUN chown -R appuser:appgroup /app /app/Data
USER appuser
ENTRYPOINT ["dotnet", "Melissa.WebServer.dll"]
