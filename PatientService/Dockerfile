# ── Stage 1: Build ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies first
# (Docker caches this layer — speeds up rebuilds)
COPY ["Services/PatientService/PatientService.csproj", "Services/PatientService/"]
RUN dotnet restore "Services/PatientService/PatientService.csproj"

# Copy all source code
COPY . .

# Build the project
WORKDIR "/src/Services/PatientService"
RUN dotnet build "PatientService.csproj" -c Release -o /app/build

# ── Stage 2: Publish ─────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "PatientService.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false

# ── Stage 3: Final Runtime Image ─────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PatientService.dll"]