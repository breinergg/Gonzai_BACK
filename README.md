# GonzAI – API

API .NET 8 (EF Core + PostgreSQL) de GonzAI. El frontend Angular vive en `frontend/Gonzai_FRONT-v2`.

## Desarrollo

```bash
dotnet user-secrets --project Gonzai_API set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=gonzai;Username=...;Password=..."
dotnet user-secrets --project Gonzai_API set "Jwt:Key" "<mínimo 32 caracteres>"
dotnet user-secrets --project Gonzai_API set "Gemini:ApiKey" "<clave>"
dotnet run --project Gonzai_API
```

Los secretos nunca van en `appsettings*.json`. Ver `Gonzai_API/appsettings.Development.example.json`.

## Pruebas

```bash
dotnet test Gonzai_API.Tests
```

Son pruebas de integración (ventas, pagos, anulaciones, stock, concurrencia, seguridad). Levantan un
PostgreSQL real con Testcontainers, por lo que **necesitan Docker en marcha**.

## Producción con Docker

```bash
cd deploy
cp .env.example .env      # completa los valores (JWT_KEY, contraseñas, dominio...)
docker compose up -d --build
```

Levanta PostgreSQL, la API y nginx con el frontend (la carpeta del frontend debe estar en
`../../frontend/Gonzai_FRONT-v2`). Al arrancar, la API aplica las migraciones y crea el primer
administrador con `ADMIN_EMAIL` / `ADMIN_PASSWORD` si no hay usuarios. Después de entrar, cambia la
contraseña y vacía `ADMIN_PASSWORD` del `.env`.

- Solo el servicio `web` publica puerto (`WEB_PORT`). La base y la API quedan en la red interna.
- Pon un terminador TLS delante (Caddy, Cloudflare, balanceador del hosting).
- `GET /health` (interno) comprueba que la API y PostgreSQL responden.
- Variables de entorno relevantes: `Jwt__Key`, `ConnectionStrings__DefaultConnection`, `Gemini__ApiKey`,
  `Cors__Origins__0`, `Comprobantes__BaseUrl`, `ForwardedHeaders__Enabled`, `HttpsRedirection__Enabled`,
  `Database__MigrateOnStartup`, `Seed__Admin*`, `Smtp__*`.
- `ForwardedHeaders__Enabled=true` **solo** si la API no es accesible directamente desde internet.
- Haz copias de seguridad del volumen `gonzai-pgdata` (`pg_dump`).
