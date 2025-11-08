# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar solo el csproj primero
COPY ProducScan.csproj .
RUN dotnet restore ProducScan.csproj

# Copiar el resto del código
COPY . .
RUN dotnet publish ProducScan.csproj -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProducScan.dll"]