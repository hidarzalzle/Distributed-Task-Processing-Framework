FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore DistributedTaskFramework.sln
RUN dotnet publish src/DistributedTaskFramework.Worker/DistributedTaskFramework.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DistributedTaskFramework.Worker.dll"]
