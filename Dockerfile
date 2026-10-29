# API de Animal Rescue (.NET 9). Render construye esta imagen y define PORT.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restaurar primero solo con los .csproj aprovecha la caché de capas.
COPY AnimalRescue.API/AnimalRescue.API.csproj AnimalRescue.API/
COPY AnimalRescue.BLL/AnimalRescue.BLL.csproj AnimalRescue.BLL/
COPY AnimalRescue.DAL/AnimalRescue.DAL.csproj AnimalRescue.DAL/
RUN dotnet restore AnimalRescue.API/AnimalRescue.API.csproj

COPY . .
RUN dotnet publish AnimalRescue.API/AnimalRescue.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p uploads && chown "$APP_UID" uploads
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-10000} exec dotnet AnimalRescue.API.dll"]
