FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/TCIP.Common/TCIP.Common.csproj src/TCIP.Common/
COPY src/TCIP.Business/TCIP.Business.csproj src/TCIP.Business/
COPY src/TCIP.Infrastructure/TCIP.Infrastructure.csproj src/TCIP.Infrastructure/
COPY src/TCIP.Api/TCIP.Api.csproj src/TCIP.Api/

RUN dotnet restore src/TCIP.Api/TCIP.Api.csproj

COPY src/ src/
RUN dotnet publish src/TCIP.Api/TCIP.Api.csproj --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "TCIP.Api.dll"]
