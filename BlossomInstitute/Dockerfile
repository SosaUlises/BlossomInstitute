# 1. Imagen base para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln ./
COPY BlossomInstitute/BlossomInstitute.csproj BlossomInstitute/
COPY BlossomInstitute.Application/BlossomInstitute.Application.csproj BlossomInstitute.Application/
COPY BlossomInstitute.Common/BlossomInstitute.Common.csproj BlossomInstitute.Common/
COPY BlossomInstitute.Domain/BlossomInstitute.Domain.csproj BlossomInstitute.Domain/
COPY BlossomInstitute.Infraestructure/BlossomInstitute.Infraestructure.csproj BlossomInstitute.Infraestructure/
COPY BlossomInstitute.UnitTests/BlossomInstitute.UnitTests.csproj BlossomInstitute.UnitTests/

# Restaurar dependencias
RUN dotnet restore

COPY . .

# Compilar y publicar
WORKDIR "/src/BlossomInstitute"
RUN dotnet publish -c Release -o /app/publish

# 2. Imagen final para ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BlossomInstitute.dll"]