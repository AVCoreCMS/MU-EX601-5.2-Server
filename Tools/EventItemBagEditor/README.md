# EventItemBag Editor

Editor gráfico de Windows para los XML de `EventItemBag` de MU EX601 5.2.

## Funciones

- Navegación por carpetas y búsqueda por nombre.
- Soporte para formato básico `<ItemBag UseEx="0">` con `<Item>`.
- Soporte para formato extendido `<ItemBag UseEx="1">` con `<Ex>`, `<Drop>`, `<Class>` y `<Pool>`.
- Edición visual de Config, items, drops, clases y pools.
- Agregar, duplicar y eliminar filas.
- Validación de rangos y referencias de pools.
- Vista/edición XML para ajustes avanzados.
- Backup automático `.bak` antes de guardar.
- Compilación como `EventItemBagEditor.exe` x64, single-file y self-contained mediante GitHub Actions.

## Uso

1. Ejecuta `EventItemBagEditor.exe`.
2. Presiona **Abrir carpeta** y selecciona la carpeta raíz `EventItemBag`.
3. Selecciona un XML del árbol de la izquierda.
4. Edita sus valores.
5. Presiona **Validar** y luego **Guardar**.

El programa no necesita Python ni archivos `.bat`.

## Compilar localmente

Requiere .NET 8 SDK en Windows:

```powershell
dotnet publish EventItemBagEditor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## GitHub Actions

El workflow `.github/workflows/event-item-bag-editor.yml` genera el artefacto `EventItemBagEditor-win-x64`, que contiene únicamente `EventItemBagEditor.exe`.
