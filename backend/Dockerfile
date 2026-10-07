# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config Directory.Build.props Directory.Packages.props UniversityLostFound.slnx .editorconfig ./
COPY src/Domain/UniversityLostFound.Domain.csproj src/Domain/
COPY src/Application/UniversityLostFound.Application.csproj src/Application/
COPY src/Infrastructure/UniversityLostFound.Infrastructure.csproj src/Infrastructure/
COPY src/Api/UniversityLostFound.Api.csproj src/Api/
RUN dotnet restore src/Api/UniversityLostFound.Api.csproj
COPY src/ src/
RUN dotnet publish src/Api/UniversityLostFound.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Npgsql probes for Kerberos (GSSAPI) at startup; without this lib it logs a misleading error.
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# No curl in the runtime image; bash's /dev/tcp is enough to ask /health for a 200.
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 \
  CMD ["bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.0\r\n\r\n' >&3 && head -n1 <&3 | grep -q ' 200 '"]
ENTRYPOINT ["dotnet", "UniversityLostFound.Api.dll"]
