# Seguridad de la plataforma web

- No existe registro público de usuarios.
- El administrador inicial se crea solamente mediante secretos del entorno.
- Las contraseñas usan el hash adaptativo de ASP.NET Core Identity; nunca se guardan en texto plano. La política actual exige al menos nueve caracteres, letras minúsculas y números.
- La sesión usa cookie `HttpOnly`, `Secure`, `SameSite=Strict` y caducidad deslizante.
- Cinco intentos fallidos bloquean temporalmente la cuenta y el acceso limita solicitudes por IP.
- La autorización se valida siempre en la API mediante roles.
- HTTPS es obligatorio fuera del entorno local.
- Los secretos reales, copias SQLite y bases PostgreSQL no pertenecen al repositorio.
