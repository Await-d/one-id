FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src

COPY backend/Directory.Packages.props ./
COPY backend/OneID.sln ./
COPY backend/OneID.Shared/OneID.Shared.csproj OneID.Shared/
COPY backend/OneID.Identity/OneID.Identity.csproj OneID.Identity/
COPY backend/OneID.AdminApi/OneID.AdminApi.csproj OneID.AdminApi/

RUN dotnet restore OneID.AdminApi/OneID.AdminApi.csproj

COPY backend/ ./

RUN dotnet publish OneID.AdminApi/OneID.AdminApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "OneID.AdminApi.dll"]
