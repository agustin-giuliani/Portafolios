
# ==========================================
# ETAPA 1: COMPILACION
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore "Portafolio.csproj"

RUN dotnet publish "Portafolio.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ==========================================
# ETAPA 2: EJECUCION
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Portafolio.dll"]
