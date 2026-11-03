# GonzAI – API

API .NET 8 (EF Core + PostgreSQL) de GonzAI. El frontend Angular vive en `frontend/Gonzai_FRONT-v2`.

## Una base de datos por empresa

GonzAI se vende a varias empresas y **cada una tiene su propia base de datos**. Una empresa no
puede ver los clientes, ventas, deudas ni el inventario de otra, ni siquiera por un error de
programación: no comparten tablas.

- `gonzai_catalogo` guarda solo la lista de empresas y a qué empresa pertenece cada correo.
- `gonzai_emp_<identificador>` guarda todo lo de esa empresa. La crea la API al darla de alta.

El correo de una persona pertenece a una sola empresa, así que el login sigue pidiendo solo
correo y contraseña: con el correo se sabe en qué base validar. La empresa viaja firmada dentro
del token JWT, y de ahí sale la conexión de cada petición.

### Dar de alta una empresa

Las rutas de plataforma se protegen con `Plataforma:ApiKey` (tuya, no de tus clientes). Si no
está configurada, quedan cerradas.

```bash
curl -X POST https://tu-dominio/api/plataforma/empresas \
  -H "X-Plataforma-ApiKey: $PLATAFORMA_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Boutique Ana","slug":"boutique","adminNombre":"Ana",
       "adminEmail":"ana@boutique.com","adminPassword":"<contraseña inicial>"}'
```

Eso crea su base, aplica las migraciones y le deja su propio administrador. Otras rutas:

| Ruta | Para qué |
|---|---|
| `GET /api/plataforma/empresas` | Listar empresas |
| `PATCH /api/plataforma/empresas/{id}/estado` | Activar o suspender (por ejemplo, si deja de pagar) |
| `POST /api/plataforma/migrar` | Aplicar migraciones pendientes a todas las empresas |

Suspender una empresa corta el acceso de inmediato, incluso con tokens ya emitidos.

### Clave de Gemini: compartida o propia por empresa

Cada empresa puede traer su propia clave gratuita de Gemini (Google ofrece una por cuenta) en vez
de usar la clave compartida de la plataforma. Se configura desde la app, en Configuración → Asistente
de IA, con el usuario Admin de esa empresa, o por API:

```bash
curl -X PUT https://tu-dominio/api/negocio/ia/gemini \
  -H "Authorization: Bearer $TOKEN_ADMIN_DE_LA_EMPRESA" \
  -H "Content-Type: application/json" \
  -d '{"apiKey":"AIza..."}'
```

- La clave se guarda **cifrada** (ASP.NET Data Protection) en la base de esa empresa; nunca se
  devuelve, solo si está configurada (`tieneClaveIaPropia`).
- Con clave propia, el consumo (y el plan gratuito) corre por cuenta de la empresa, y el
  `LimiteChatsDiarios` de la plataforma deja de aplicarle.
- Sin clave propia, se usa `Gemini:ApiKey` (la compartida) y sí aplica el `LimiteChatsDiarios` de esa empresa.
- Si ninguna de las dos existe, el chat responde con un mensaje claro en vez de fallar: pide que un
  administrador configure una clave propia.
- El llavero de cifrado se guarda en el catálogo (tabla `DataProtectionKeys`), no en el disco del
  contenedor: sobrevive a los reinicios. Si se perdiera, las claves cifradas quedarían ilegibles —
  haz copia del catálogo junto con el resto de respaldos.

### Adoptar una base que ya existe

Para la primera empresa, o para restaurar un respaldo, se registra una base existente sin copiar
nada: se le aplican las migraciones y se inscriben en el directorio los correos de sus usuarios,
que siguen entrando con su contraseña de siempre. Se hace con `Seed:Empresa:BaseDatosExistente`
(variable `SEED_BASE_EXISTENTE` en Docker).

### Respaldos

Cada empresa se respalda y se restaura por separado:

```bash
docker exec gonzai-db-1 pg_dump -U gonzai gonzai_emp_boutique > boutique.sql
docker exec gonzai-db-1 pg_dump -U gonzai gonzai_catalogo     > catalogo.sql
```

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

Para crear o cambiar migraciones hay dos contextos, así que hay que decir cuál:

```bash
dotnet ef migrations add <Nombre> --project Gonzai_API --context AppDbContext
dotnet ef migrations add <Nombre> --project Gonzai_API --context PlataformaDbContext --output-dir Migrations/Plataforma
```

Son 43 pruebas de integración (ventas, pagos, anulaciones, stock, concurrencia, seguridad y
aislamiento entre empresas). Levantan un PostgreSQL real con Testcontainers y dan de alta dos
empresas distintas para comprobar que ninguna ve los datos de la otra, por lo que **necesitan
Docker en marcha**.

## Producción con Docker

```bash
cd deploy
cp .env.example .env      # completa los valores (JWT_KEY, contraseñas, dominio...)
docker compose up -d --build
```

Levanta PostgreSQL, la API y nginx con el frontend (la carpeta del frontend debe estar en
`../../frontend/Gonzai_FRONT-v2`). Al arrancar, la API migra el catálogo, crea la primera empresa
con `ADMIN_EMAIL` / `ADMIN_PASSWORD` si el catálogo está vacío, y pone al día las migraciones de
todas las empresas. Después de entrar, cambia la contraseña y vacía `ADMIN_PASSWORD` del `.env`.

- Solo el servicio `web` publica puerto (`WEB_PORT`). La base y la API quedan en la red interna.
- Pon un terminador TLS delante (Caddy, Cloudflare, balanceador del hosting).
- `GET /health` (interno) comprueba que la API y PostgreSQL responden.
- Variables de entorno relevantes: `Jwt__Key`, `ConnectionStrings__DefaultConnection`, `Gemini__ApiKey`,
  `Cors__Origins__0`, `Comprobantes__BaseUrl`, `ForwardedHeaders__Enabled`, `HttpsRedirection__Enabled`,
  `Database__MigrateOnStartup`, `Plataforma__ApiKey`, `Seed__Empresa__*`, `Seed__Admin*`, `Smtp__*`,
  `Tenancy__CatalogDatabase`, `Tenancy__DatabasePrefix`, `Tenancy__MaxPoolSize`.
- `ForwardedHeaders__Enabled=true` **solo** si la API no es accesible directamente desde internet.
- El usuario de PostgreSQL debe poder crear bases de datos: la API crea una por empresa.
- Haz copias de seguridad del volumen `gonzai-pgdata`, o un `pg_dump` por empresa (ver arriba).
