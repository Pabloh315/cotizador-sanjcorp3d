# Instalación y distribución

## Usuario final

Ejecuta `SANJ-CORP-3D-Setup-v1.0.0.exe`. Se instala sin permisos de administrador en:

`%LocalAppData%\Programs\SanjCorp3D`

El instalador crea un acceso en el menú Inicio y, si se selecciona, otro en el escritorio. Windows puede mostrar una advertencia de SmartScreen porque la versión 1.0.0 no está firmada con un certificado de firma de código.

La desinstalación está disponible en Configuración de Windows y conserva la base ubicada en `%LocalAppData%\SanjCorp3D`.

## Desarrollo

Ejecuta `scripts/build.ps1` para restaurar con el archivo de bloqueo, compilar y correr la prueba integral. Ejecuta `scripts/build-installer.ps1` para publicar un ejecutable autónomo de Windows x64 y construir el instalador.
