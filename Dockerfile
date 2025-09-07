FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["EthCrawlerApi.csproj", "./"]
RUN dotnet restore "./EthCrawlerApi.csproj"

COPY . .
RUN dotnet publish "./EthCrawlerApi.csproj" -c Release -o /app/out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "EthCrawlerApi.dll"]
