# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia solução e csprojs para restaurar em cache
COPY *.sln ./
COPY src/Rag.Core/Rag.Core.csproj src/Rag.Core/
COPY src/Rag.Infrastructure/Rag.Infrastructure.csproj src/Rag.Infrastructure/
COPY src/Rag.App/Rag.App.csproj src/Rag.App/

# Restore
RUN dotnet restore ./src/Rag.App/Rag.App.csproj

# Copia o restante do código
COPY src/ ./src/

# Build e publish
RUN dotnet publish ./src/Rag.App/Rag.App.csproj -c Release -o /app/out /p:UseAppHost=false

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/out ./

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Rag.App.dll"]