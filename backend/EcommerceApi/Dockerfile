FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EcommerceApi.csproj", "."]
RUN dotnet restore --disable-parallel
COPY . .
RUN dotnet build -c Release -o /app/build /p:UseSharedCompilation=false

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseSharedCompilation=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "EcommerceApi.dll"]
