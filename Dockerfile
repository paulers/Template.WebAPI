# Copy CSPROJ and restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
RUN dotnet restore Template.WebAPI.sln

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish src/Template.WebAPI.Api/Template.WebAPI.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
EXPOSE 8080
EXPOSE 8443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Template.WebAPI.Api.dll"]
