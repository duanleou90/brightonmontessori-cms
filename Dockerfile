#
# Multi-stage build for Umbraco (ASP.NET Core)
#

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore first (better layer caching)
COPY brightonmontessori-cms.csproj ./
RUN dotnet restore "./brightonmontessori-cms.csproj"

# Copy everything else and publish
COPY . ./
RUN dotnet publish "./brightonmontessori-cms.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Azure Web App for Containers will forward traffic to the port you listen on.
# HTTP on 8080 for production (Azure handles TLS termination)
# HTTPS on 8443 for local development with Umbraco backoffice
ENV ASPNETCORE_URLS="http://+:8080;https://+:8443"
ENV ASPNETCORE_Kestrel__Certificates__Default__Password="devcert"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path="/https/aspnetapp.pfx"
EXPOSE 8080 8443

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "brightonmontessori-cms.dll"]
