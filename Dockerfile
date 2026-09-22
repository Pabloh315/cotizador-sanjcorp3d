# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS web-build
WORKDIR /src
COPY Web/sanjcorp3d-web/package.json Web/sanjcorp3d-web/package-lock.json ./
RUN npm ci
COPY Web/sanjcorp3d-web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
# Bump this revision when a migration or API source is added so hosted
# BuildKit caches cannot reuse an image built without the new files.
ARG BUILD_REVISION=consumable-response-circular-fix-v3
RUN echo "Building API revision ${BUILD_REVISION}"
COPY Web/SanjCorp3D.Api/SanjCorp3D.Api.csproj Web/SanjCorp3D.Api/
RUN dotnet restore Web/SanjCorp3D.Api/SanjCorp3D.Api.csproj
COPY Web/SanjCorp3D.Api/ Web/SanjCorp3D.Api/
RUN dotnet publish Web/SanjCorp3D.Api/SanjCorp3D.Api.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api-build /out ./
COPY --from=web-build /src/dist ./wwwroot
RUN mkdir -p /var/lib/sanjcorp3d/keys && chown -R app:app /var/lib/sanjcorp3d
USER app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DataProtection__KeysPath=/var/lib/sanjcorp3d/keys
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet SanjCorp3D.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
