# Deuda técnica

Registro de cosas que sabemos que hay que mejorar pero que, conscientemente, dejamos
para más adelante por estar en fase de prototipo con datos simulados.

Cada entrada indica **qué**, **por qué se pospone** y **cuándo hay que abordarla**.

## Seguridad

### 1. Autenticación y autorización en la API _(prioridad alta)_

Todos los endpoints de [`SalesController`](backend/Infrastructure/Inbound/Http/SalesController.cs)
son anónimos, y el mock expone el fichero sin control de acceso.

- **Por qué se pospone:** hoy solo se sirven datos ficticios (mock) o de demo (el sandbox del
  Business Accelerator Hub, no sensibles) y el sistema corre en local.
- **Cuándo abordarla:** **antes** de apuntar el adaptador SAP (`SapODataSalesRepository`) a un
  tenant con **datos reales** de negocio o de desplegar fuera de `localhost`. Sin esto, se
  expondrían datos reales sin protección. CLAUDE.md ya anticipa el comportamiento 401/403 de la API .NET.

### 2. Hardening del contenedor de frontend

El [`frontend/Dockerfile`](frontend/Dockerfile) corre en modo desarrollo (`npm run dev`) y como
root.

- **Por qué se pospone:** es un contenedor de iteración con hot-reload; meterle usuario no-root
  ahora complica los permisos del volumen y del directorio `.next/`.
- **Cuándo abordarla:** junto a la migración a multi-stage de producción que el propio Dockerfile
  documenta (`npm ci` + `npm run build` + `npm start` + `output: 'standalone'`). En ese momento
  se añade también el usuario no-root (`USER node`).

> Backend y `sap-mock` **ya corren como no-root** (usuario `app` y `nginx-unprivileged` UID 101).

### 3. Fijar imágenes base por digest

Los Dockerfiles usan tags móviles (`dotnet/aspnet:10.0`, `nginxinc/nginx-unprivileged:alpine`,
`node:20-alpine`).

- **Por qué se pospone:** comodidad durante el prototipado.
- **Cuándo abordarla:** al preparar despliegue, fijar por `@sha256:...` para reproducibilidad y
  cadena de suministro.

## Rendimiento / escalabilidad

### 4. Lectura del fichero completo en memoria

[`MockTxtSalesRepository`](backend/Infrastructure/Outbound/MockTxt/MockTxtSalesRepository.cs)
hace `GetByteArrayAsync` + `GetString` + LINQ: carga el fichero entero en memoria.

- **Por qué se pospone:** la fixture actual es minúscula.
- **Cuándo abordarla:** cuando el origen pase a "grandes cantidades de datos" reales. Con SAP real
  conviene streaming / paginación / agregación empujada al origen (SQL `GROUP BY`, OData `$apply`),
  que es justo lo previsto para el paso 3 del plan (mover la agregación al puerto).
- **Nota:** los datos ingeridos ya **se persisten** en SQLite (`ISalesStore` / `SqliteSalesStore`),
  así que las consultas no recargan la fuente; pero la **ingesta** sí sigue leyendo el payload
  completo en memoria. Lo pendiente es el streaming en esa fase de ingesta.

## Robustez

### 5. Manejo global de excepciones

No hay middleware de manejo de errores en [`Program.cs`](backend/Program.cs).

- **Por qué no es urgente:** la imagen `dotnet/aspnet` arranca en `Production` por defecto, así que
  no se filtran stack traces.
- **Cuándo abordarla:** al desplegar, añadir manejo de errores consistente y asegurar que
  `ASPNETCORE_ENVIRONMENT` nunca es `Development` en entornos accesibles.

---

_Última revisión: 2026-05-24._
