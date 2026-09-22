# Lista de verificación de versión

- [ ] Actualizar versión en ambos archivos `.csproj` y en `CHANGELOG.md`.
- [ ] Ejecutar `scripts/build.ps1` sin errores ni advertencias.
- [ ] Ejecutar `scripts/build-installer.ps1`.
- [ ] Probar `artifacts/app/CotizadorSanjCorp3D.exe --self-test`.
- [ ] Probar `artifacts/installer/SANJ-CORP-3D-Setup-v1.0.0.exe --self-test`.
- [ ] Revisar las capturas de tamaños normal y compacto.
- [ ] Confirmar que no hay `.db`, respaldos, exportaciones ni ajustes personales.
- [ ] Calcular y publicar el SHA-256 del instalador.
- [ ] Firmar el instalador cuando exista un certificado de firma de código.
