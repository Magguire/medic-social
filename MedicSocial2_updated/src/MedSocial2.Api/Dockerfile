FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MedSocial2.sln ./
COPY src/MedSocial2.Api/MedSocial2.Api.csproj src/MedSocial2.Api/
RUN dotnet restore src/MedSocial2.Api/MedSocial2.Api.csproj

COPY . .
RUN dotnet publish src/MedSocial2.Api/MedSocial2.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MedSocial2.Api.dll"]
