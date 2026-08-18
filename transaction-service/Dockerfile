FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TransactionService.csproj .
RUN dotnet restore TransactionService.csproj
COPY . .
RUN dotnet publish TransactionService.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 5002
ENTRYPOINT ["dotnet", "TransactionService.dll"]
