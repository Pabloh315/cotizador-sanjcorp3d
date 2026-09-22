# Arquitectura

La solución usa .NET 8 Windows Forms y separa responsabilidades bajo `CotizadorSanjCorp3D/Modern`:

- `Domain`: modelos de negocio y estructuras de cotización.
- `Services`: cálculo, estado, rutas, exportación, pruebas y capturas de interfaz.
- `Infrastructure`: esquema y operaciones de SQLite.
- `UI`: tema, ventana principal, diálogos y páginas.

El cálculo se mantiene separado de la persistencia. Esto permite sustituir SQLite por un repositorio remoto en una fase futura sin trasladar reglas de negocio a la interfaz.

El logo se incrusta en los ensamblados; el ejecutable no depende de archivos visuales externos. El instalador incrusta a su vez el ejecutable autónomo publicado.
