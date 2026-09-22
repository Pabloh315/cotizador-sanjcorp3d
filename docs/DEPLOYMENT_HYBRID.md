# Despliegue hibrido gratuito

Arquitectura:

- Base de datos: Supabase PostgreSQL.
- Backend API: Koyeb o Northflank usando `Dockerfile.api`.
- Frontend: Cloudflare Pages o Vercel desde `Web/sanjcorp3d-web`.

## 1. Base de datos

La base debe estar migrada con:

```powershell
.\scripts\apply-supabase-schema.ps1
```

Usar la URI Session pooler de Supabase:

```text
postgresql://postgres.PROJECT_REF:PASSWORD@aws-...pooler.supabase.com:5432/postgres
```

## 2. Backend API

Configurar el servicio con:

- Dockerfile: `Dockerfile.api`
- Puerto interno: `8080`
- Health check: `/health`

Variables requeridas:

```text
DATABASE_URL=postgresql://postgres.PROJECT_REF:PASSWORD@aws-...pooler.supabase.com:5432/postgres
SeedUsers__super_admin__Password=CAMBIAR_POR_PASSWORD_SEGURA
Cors__Origins=https://URL-DEL-FRONTEND
```

Mientras el frontend aun no exista, se puede dejar temporalmente:

```text
Cors__Origins=https://cotizadoronline.vercel.app
```

Despues de publicar el frontend, actualizar `Cors__Origins` con la URL real.

## 3. Frontend

Directorio raiz del frontend:

```text
Web/sanjcorp3d-web
```

Comando de build:

```text
npm run build
```

Directorio de salida:

```text
dist
```

Variable requerida:

```text
VITE_API_URL=https://URL-DEL-BACKEND
```

Cloudflare Pages usa `public/_redirects` para rutas SPA.
Vercel usa `vercel.json` para rutas SPA.

## 4. Prueba final

1. Abrir `https://URL-DEL-BACKEND/health` y confirmar `{ "status": "ok" }`.
2. Abrir el frontend.
3. Iniciar sesion con `super_admin` y la password definida en `SeedUsers__super_admin__Password`.
4. Cambiar/crear usuarios reales desde la app.