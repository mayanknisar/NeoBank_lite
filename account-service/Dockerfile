FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AccountService.csproj .
RUN dotnet restore AccountService.csproj
COPY . .
RUN dotnet publish AccountService.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 5001 6001
ENTRYPOINT ["dotnet", "AccountService.dll"]
