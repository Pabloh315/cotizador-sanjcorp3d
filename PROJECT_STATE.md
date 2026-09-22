# Estado del proyecto

Fecha: 2026-08-24

## Estado actual

- Aplicación modernizada de .NET Framework 4.7.2 a .NET 8 Windows Forms.
- Datos persistentes en SQLite local fuera del repositorio.
- Ocho páginas funcionales: Cotizador, Impresoras, Consumibles, Materiales, Historial, Reportes, Configuración y Ayuda.
- Cotización multicolor con precio propio por filamento.
- Consumibles incluye catálogo separado de Filamentos y Resinas; SLA pertenece exclusivamente a Resinas.
- Inventario manual por tipo y color, con cantidad de rollos o botellas y alerta de existencia cero.
- Mantenimiento por impresión y multiplicador configurables; el costo por uso de impresora fue retirado.
- Mano de obra y recargo funcional retirados del modelo por decisión de negocio.
- Contenedores de texto adaptables a escala DPI para evitar recortes.
- Cotizaciones recuperables con todos sus datos por código, cliente o proyecto.
- Códigos copiables, sin separadores, con iniciales, fecha y número correlativo.
- Confirmación de ventas y reporte de ingresos y ganancias realizadas.
- Fórmula configurable, historial, reportes, CSV y respaldos.
- Código antiguo conservado fuera de la lista de compilación y ZIP original sin modificar.
- Compilación Release: 0 errores, 0 advertencias.
- Prueba integral `--self-test`: aprobada.
- Auditoría de dependencias: sin paquetes vulnerables conocidos.
- Logo oficial integrado como recurso en la aplicación, icono de ventana e instalador.
- Versión 1.0.0 publicada como ejecutable autónomo e instalador por usuario para Windows x64.
- Carpeta independiente y saneada preparada para cargar a GitHub, con documentación, scripts y validación continua.

## Arquitectura

- `Modern/Domain`: modelos sin dependencia de interfaz.
- `Modern/Services`: cálculo, rutas, CSV, estado y prueba integral.
- `Modern/Infrastructure`: repositorio SQLite y esquema versionable.
- `Modern/UI`: tema, ventana principal, diálogos y páginas.

## Próxima evolución prevista

- Importación de 3MF/G-code generado por laminadores.
- Vista previa STL/3MF y estimación a partir de perfiles.
- Repositorio remoto MongoDB detrás de una interfaz de persistencia.
- Autenticación y sincronización cuando se habilite el servidor.
