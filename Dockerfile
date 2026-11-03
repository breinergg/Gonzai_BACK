# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Gonzai_API/Gonzai_API.csproj Gonzai_API/
RUN dotnet restore Gonzai_API/Gonzai_API.csproj
COPY Gonzai_API/ Gonzai_API/
RUN dotnet publish Gonzai_API/Gonzai_API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
# QuestPDF (PDF de comprobantes) necesita fontconfig y al menos una fuente; curl es para el healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Gonzai_API.dll"]
