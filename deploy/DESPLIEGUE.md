# Desplegar GonzAI gratis en Oracle Cloud Always Free

Guía paso a paso para dejar GonzAI corriendo en internet, con HTTPS, sin pagar nada.

## Por qué Oracle Cloud y no Vercel / Supabase / Render

GonzAI crea una base de datos de PostgreSQL nueva (`CREATE DATABASE`) cada vez que se da de
alta una empresa. Eso exige un servidor Postgres propio, con permisos de superusuario — algo
que ningún Postgres "gestionado" gratis (Supabase, Render, etc.) permite: te dan una sola base
ya creada, sin permiso para crear otras dentro de la misma cuenta. Vercel, por su parte, no
corre contenedores de larga duración ni gestiona bases de datos.

Oracle Cloud Always Free es la única oferta gratuita que da una VM real (hasta 2 OCPU / 12 GB
de RAM, con Postgres propio dentro) sin fecha de caducidad.

**Aviso honesto:** en 2026 Oracle redujo esa cuota (antes era 4 OCPU/24 GB) y, sobre todo, **crear
la VM a veces falla con "Out of host capacity"** porque la depara de un fondo compartido de
máquinas ARM que se agota seguido. No es un límite de tu cuenta: es que en ese momento no hay
máquinas libres en esa región. La solución es simplemente reintentar — a veces funciona al
segundo intento, a veces hay que insistir un rato. 2 OCPU / 12 GB es, de sobra, suficiente para
esta app (lo usamos con mucho menos en desarrollo).

## Antes de empezar

- Una tarjeta de crédito o débito real (no prepago ni virtual) para verificar la cuenta de
  Oracle. No te van a cobrar mientras uses solo recursos "Always Free".
- Tu clave de Gemini (la misma que usas en desarrollo).
- Un rato de paciencia por si hay que reintentar la creación de la VM.

---

## 1. Cuenta y VM en Oracle Cloud

1. Entra a [cloud.oracle.com/free](https://www.oracle.com/cloud/free/) y crea la cuenta.
2. En la consola: **Compute → Instances → Create instance**.
3. Nombre: `gonzai-prod` (o el que prefieras).
4. **Imagen**: cambia la que viene por defecto. Elige **Canonical Ubuntu 24.04** en su
   variante **aarch64** (ARM) — es importante que sea la versión ARM, no x86.
5. **Forma (shape)**: *Change shape* → categoría **Ampere** → `VM.Standard.A1.Flex`. Configura
   2 OCPU y 12 GB de RAM (o el máximo que tu cuenta permita sin salir de "Always Free" — la
   consola te avisa si te pasas).
6. **Claves SSH**: deja que Oracle genere el par de llaves y descarga la privada
   (`ssh-key-...key`), o sube tu propia llave pública si ya tienes una
   (`ssh-keygen -t ed25519` en tu Mac si no tienes).
7. **Create**. Si sale **"Out of host capacity"**: cambia el *Availability Domain* si tu región
   tiene varios, y vuelve a intentar. Si sigue fallando, espera unos minutos y reintenta — es
   normal que tome varios intentos.
8. Cuando quede lista, anota la **IP pública** de la instancia.

## 2. Abrir los puertos (dos lugares distintos — es fácil olvidar el segundo)

**A) En la consola de Oracle** (firewall de red):
En la página de la instancia → la subred (*Subnet*) → **Security List** → **Add Ingress
Rules**. Añade dos reglas, origen `0.0.0.0/0`:
- Puerto **80** (HTTP)
- Puerto **443** (HTTPS)

(El puerto 22 para SSH ya suele venir abierto por defecto.)

**B) Dentro de la VM** (firewall del sistema operativo — Ubuntu en Oracle trae `iptables`
activo por defecto, y aunque abras el puerto en la consola, Ubuntu lo sigue bloqueando si no
lo abres aquí también):

```bash
sudo iptables -I INPUT -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT -p tcp --dport 443 -j ACCEPT
sudo netfilter-persistent save
```

## 3. Dominio gratis

1. Crea una cuenta gratis en [duckdns.org](https://www.duckdns.org).
2. Crea un subdominio, por ejemplo `gonzai.duckdns.org`, y apúntalo a la IP pública de tu VM.
3. Espera 1-2 minutos y confirma que resuelve: `ping gonzai.duckdns.org` desde tu Mac.

## 4. Conectar y preparar el servidor

```bash
ssh -i ruta/a/tu-clave.key ubuntu@IP_DE_TU_VM

# Ya dentro de la VM:
sudo apt update && sudo apt upgrade -y
sudo apt install -y git

# Docker
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
newgrp docker
```

## 5. Clonar el proyecto

```bash
mkdir -p ~/gonzai && cd ~/gonzai
git clone https://github.com/breinergg/Gonzai_BACK.git backend/Gonzai_BACK
git clone https://github.com/breinergg/Gonzai_FRONT-v2.git frontend/Gonzai_FRONT-v2
cd backend/Gonzai_BACK/deploy
cp .env.example .env
nano .env
```

Completa en `.env`:

| Variable | Valor |
|---|---|
| `POSTGRES_PASSWORD` | una contraseña nueva, cualquiera larga |
| `JWT_KEY` | genera con `openssl rand -base64 48` |
| `PLATAFORMA_API_KEY` | genera con `openssl rand -base64 32` (guárdala — la usarás para dar de alta empresas) |
| `GEMINI_API_KEY` | tu clave real de Gemini |
| `SEED_EMPRESA_NOMBRE` / `SEED_EMPRESA_SLUG` | el nombre de tu empresa |
| `ADMIN_EMAIL` / `ADMIN_PASSWORD` | tus credenciales de acceso |
| `PUBLIC_URL` | `https://gonzai.duckdns.org` (tu dominio real) |
| `PUBLIC_HOST` | `gonzai.duckdns.org` (sin `https://`) |

Si ya tienes datos reales en una base (como la que dejamos lista localmente), en vez de
`ADMIN_EMAIL`/`ADMIN_PASSWORD` puedes rellenar `SEED_BASE_EXISTENTE` con el nombre de esa base
y restaurarla en el servidor (ver sección 8).

## 6. Levantar todo

```bash
docker compose up -d --build
docker compose ps
```

Debe mostrar `db`, `api`, `web` y `caddy` como `healthy` o `running`. La primera vez Caddy
tarda unos segundos en obtener el certificado de Let's Encrypt — revísalo con:

```bash
docker compose logs -f caddy
```

Busca la línea `certificate obtained successfully`.

## 7. Verificación final

- Entra a `https://gonzai.duckdns.org` desde tu navegador — debe cargar GonzAI con el candado
  verde (certificado válido).
- Inicia sesión con el Admin que configuraste.
- Revisa `https://gonzai.duckdns.org/api/usuario/login` (debe dar 405 Method Not Allowed a un
  GET simple — confirma que la API responde detrás del proxy).

## 8. Si vas a migrar la base que ya tienes lista (no empezar de cero)

```bash
# Desde tu Mac, con el contenedor de desarrollo corriendo:
docker exec gonzai-db pg_dump -U postgres -d gonzai > gonzai.sql
scp -i tu-clave.key gonzai.sql ubuntu@IP_DE_TU_VM:~/

# En el servidor, ANTES de dar de alta nada (con docker compose ya levantado, db sana):
cd ~/gonzai/backend/Gonzai_BACK/deploy
docker compose exec -T db psql -U gonzai -d postgres -c 'CREATE DATABASE gonzai'
cat ~/gonzai.sql | docker compose exec -T db psql -U gonzai -d gonzai
```

Luego, en `.env`, pon `SEED_BASE_EXISTENTE=gonzai` y reinicia: `docker compose up -d`. La API
la adopta como la primera empresa sin tocar ni una fila.

## Mantenimiento

- **Respaldo periódico** (agrégalo a un cron semanal):
  ```bash
  docker compose exec db pg_dump -U gonzai gonzai_catalogo > catalogo-$(date +%F).sql
  docker compose exec db pg_dump -U gonzai gonzai_emp_<slug-de-tu-empresa> > empresa-$(date +%F).sql
  ```
- **Actualizar tras un cambio de código**:
  ```bash
  cd ~/gonzai/backend/Gonzai_BACK && git pull
  cd ~/gonzai/frontend/Gonzai_FRONT-v2 && git pull
  cd ~/gonzai/backend/Gonzai_BACK/deploy && docker compose up -d --build
  ```
- **Dar de alta una empresa nueva**: ver el `README.md` de este repo, sección "Dar de alta una
  empresa" — un solo `curl` con tu `PLATAFORMA_API_KEY`.
- Todos los servicios tienen `restart: unless-stopped`: si la VM se reinicia, Docker los levanta
  solo (siempre que el propio Docker arranque al iniciar el sistema, que es el comportamiento
  por defecto tras instalarlo con el script oficial).
