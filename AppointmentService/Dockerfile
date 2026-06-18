FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Services/AppointmentService/AppointmentService.csproj", "Services/AppointmentService/"]
RUN dotnet restore "Services/AppointmentService/AppointmentService.csproj"

COPY . .
WORKDIR "/src/Services/AppointmentService"
RUN dotnet build "AppointmentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AppointmentService.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppointmentService.dll"]