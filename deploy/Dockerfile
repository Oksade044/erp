# ERP API konteyner obrazı (TDD §32). Multi-stage: build → publish → runtime.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Yalnız csproj-ları köçür (restore keşi üçün)
COPY ERP.slnx ./
COPY src/ ./src/
RUN dotnet restore src/Api/ERP.Api/ERP.Api.csproj
RUN dotnet publish src/Api/ERP.Api/ERP.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ERP.Api.dll"]
