# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia solução e csprojs para restaurar em cache
COPY *.sln ./
# Ajuste os caminhos dos csprojs conforme seu repo:
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
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Opcional: fuso horário e certificados raiz (útil em ambientes corporativos)
# RUN apt-get update && apt-get install -y tzdata ca-certificates && rm -rf /var/lib/apt/lists/*

# Copia artefatos
COPY --from=build /app/out ./

# Porta padrão Kestrel do app (no compose vamos mapear 8081:8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Ambiente padrão Production (o Program.cs faz remapeamento de hostnames quando em container e não é Development)
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Rag.App.dll"]