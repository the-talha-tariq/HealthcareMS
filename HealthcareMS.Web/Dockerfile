FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Web/HealthcareMS.Web/HealthcareMS.Web.csproj", "Web/HealthcareMS.Web/"]
RUN dotnet restore "Web/HealthcareMS.Web/HealthcareMS.Web.csproj"

COPY . .
WORKDIR "/src/Web/HealthcareMS.Web"
RUN dotnet build "HealthcareMS.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HealthcareMS.Web.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HealthcareMS.Web.dll"]