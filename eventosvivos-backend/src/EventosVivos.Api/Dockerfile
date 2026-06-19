# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY EventosVivos.sln ./
COPY src/EventosVivos.Domain/EventosVivos.Domain.csproj         src/EventosVivos.Domain/
COPY src/EventosVivos.Application/EventosVivos.Application.csproj  src/EventosVivos.Application/
COPY src/EventosVivos.Infrastructure/EventosVivos.Infrastructure.csproj src/EventosVivos.Infrastructure/
COPY src/EventosVivos.Api/EventosVivos.Api.csproj                src/EventosVivos.Api/
COPY tests/EventosVivos.Tests/EventosVivos.Tests.csproj          tests/EventosVivos.Tests/

RUN dotnet restore

COPY . .
RUN dotnet publish src/EventosVivos.Api/EventosVivos.Api.csproj \
    -c Release -o /app/out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "EventosVivos.Api.dll"]
