# SANJ CORP 3D Web

Nueva plataforma web del cotizador, compuesta por:

- `SanjCorp3D.Api`: API ASP.NET Core 10, autenticación, autorización y PostgreSQL.
- `sanjcorp3d-web`: cliente React + TypeScript.
- `SanjCorp3D.Migrator`: migración verificable desde la base SQLite original.

La aplicación de escritorio y su base original se conservan sin cambios durante la transición.

## Seguridad

No existe registro público. El administrador inicial se configura mediante variables de entorno y luego crea las cuentas del equipo. La API usa cookies seguras, bloqueo por intentos, limitación de solicitudes y roles `Administrator`, `Sales`, `Production` y `Viewer`.

## Desarrollo

1. Crear una base PostgreSQL vacía.
2. Copiar las variables de `.env.example` al almacén seguro del entorno; nunca guardar contraseñas reales en archivos del repositorio.
3. Ejecutar `dotnet tool restore` y `dotnet ef database update --project Web/SanjCorp3D.Api`.
4. Iniciar la API con `dotnet run --project Web/SanjCorp3D.Api`.
5. Iniciar React desde `Web/sanjcorp3d-web` con `npm run dev`.

En este equipo, `scripts/start-web.ps1` inicia PostgreSQL aislado, la API y React sin exponer credenciales. `scripts/stop-web.ps1` los detiene de forma ordenada.

## Migración de datos

La ejecución sin `--apply` abre SQLite en modo de solo lectura, ejecuta `PRAGMA integrity_check` y cuenta todas las filas:

```powershell
dotnet run --project Web/SanjCorp3D.Migrator
```

La migración real requiere una base PostgreSQL vacía y la variable `SANJCORP_POSTGRES`. Se ejecuta dentro de una transacción y al final compara los conteos de las ocho tablas de negocio:

```powershell
dotnet run --project Web/SanjCorp3D.Migrator -- --apply
```
