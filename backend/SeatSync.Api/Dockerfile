FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SeatSync.Api/SeatSync.Api.csproj", "SeatSync.Api/"]
COPY ["SeatSync.Core/SeatSync.Core.csproj", "SeatSync.Core/"]
COPY ["SeatSync.Infrastructure/SeatSync.Infrastructure.csproj", "SeatSync.Infrastructure/"]
RUN dotnet restore "SeatSync.Api/SeatSync.Api.csproj"

COPY . .
WORKDIR /src/SeatSync.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SeatSync.Api.dll"]