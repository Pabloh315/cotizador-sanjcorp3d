# Contribución

1. Crea una rama corta desde `main`.
2. Mantén los cambios acotados y no agregues bases de datos, respaldos o datos personales.
3. Conserva tipos anulables habilitados y valida las entradas en los límites de la aplicación.
4. Ejecuta `.\scripts\build.ps1` antes de abrir un pull request.
5. Describe el comportamiento modificado y adjunta capturas cuando cambie la interfaz.

Los cambios de esquema deben conservar compatibilidad con las bases locales existentes o incluir una migración explícita.
