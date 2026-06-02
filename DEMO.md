# Demo en vivo: cómo está montada

Referencia de cómo funciona la demo pública, de dónde salen los datos y cómo usar el backend
real si hace falta. **Resumen en una frase:** la demo vive **solo en Vercel** y es
**autosuficiente** (datos de ejemplo embebidos), así que no depende de ningún backend ni sufre
cold-starts.

## Decisión: datos embebidos en el frontend

El dashboard **calcula todos los agregados en cliente** (KPIs, series, by-product, by-customer)
a partir de las ventas crudas. Eso permitió desacoplar la demo del backend: en vez de depender de
un servicio .NET + mock que en el tier gratuito **se duermen** (Render free duerme tras ~15 min y
provocaba el clásico "No hay datos" / 502 al abrir en frío), el frontend trae un **dataset de
ejemplo embebido** y lo usa directamente.

Resultado: la demo **carga al instante, siempre**, sin Render, sin cold-start, sin servicios que
mantener despiertos. El backend real sigue existiendo (es el producto), pero es **opcional** para
la demo.

## Dónde está desplegado el frontend

- **Hosting:** Vercel (siempre activo, sin cold-start).
- **URL:** <https://connect-analyzer.vercel.app>
- **Proyecto:** Next.js (App Router) en `frontend/`. Vercel hace auto-deploy en cada push a `main`
  (root directory = `frontend`).

## Cómo coge los datos de prueba

1. **Dataset embebido:** `frontend/app/lib/sample-sales.json` — las 25 ventas ficticias del mock
   (mismas fixtures que `backend/mocks/sap/data/sales.txt`, ya parseadas a JSON).
2. **`fetchDashboard()`** (`frontend/app/lib/dashboard.ts`) decide la fuente:
   - Si `BACKEND_URL` apunta a un backend **alcanzable y con datos** → usa esos datos en vivo.
   - En cualquier otro caso (sin `BACKEND_URL`, backend caído/dormido, respuesta vacía o timeout)
     → **cae automáticamente al dataset embebido**.
3. El route handler `frontend/app/api/dashboard/route.ts` (`GET`) reexpone esto al cliente
   mismo-origen; el componente `Dashboard` deriva KPIs/gráficos del set resultante y aplica los
   filtros en cliente.

**En la demo de Vercel, `BACKEND_URL` está vacío** → siempre sirve el dataset embebido → instantáneo.

### Regenerar el dataset embebido

Si cambian las fixtures del mock y quieres refrescar el JSON embebido, con el backend local
levantado en Mock:

```bash
curl -s http://localhost:5080/api/sales | python3 -m json.tool > frontend/app/lib/sample-sales.json
```

## El backend (opcional): dónde está y cómo usarlo

El backend .NET 10 (`backend/`, arquitectura hexagonal) **no es necesario para la demo**, pero es
el producto real y se puede usar/desplegar.

### En local (con datos en vivo del mock, SAP o Shopify)

```bash
docker compose up --build         # levanta sap-mock + backend + frontend
# frontend en :3000 → BACKEND_URL=http://backend:8080 (lo pone docker-compose)
```

La fuente se elige con la env var **`SalesSource`** (ver `backend/CLAUDE.md` y `.env.example`):

- `Mock` (por defecto) — lee el `.txt` del servicio `sap-mock`.
- `Sap` — OData real del SAP Business Accelerator Hub (requiere el secreto `Sap__ApiKey`).
- `Shopify` — Admin API real (requiere `Shopify__StoreUrl`, `Shopify__ClientId`,
  `Shopify__ClientSecret`).

Los secretos van en un `.env` en la raíz (gitignored); nunca en git.

### Conectar la demo a un backend en vivo

Si algún día quieres que la demo de Vercel use un backend real en vez del dataset embebido:

1. Despliega el backend (+ mock si usas `SalesSource=Mock`) — ver [`DEPLOY.md`](./DEPLOY.md)
   (Render Blueprint). Para SAP/Shopify, configura los secretos en el *Environment* del servicio.
2. En Vercel, define la env var **`BACKEND_URL`** apuntando a la URL pública del backend y
   **Redeploy**.
3. El frontend usará el backend si responde con datos; si no, seguirá cayendo al dataset embebido.

> Aviso: en hosting gratuito (Render) el backend se duerme y la primera petición tarda en
> despertar. Por eso, para una demo siempre-activa, lo recomendado es **dejar `BACKEND_URL` vacío**
> y servir el dataset embebido.

## Mapa rápido

| Pieza | Dónde | Necesaria para la demo |
|-------|-------|------------------------|
| Frontend (Next.js) | Vercel · `frontend/` | **Sí** |
| Dataset de ejemplo | `frontend/app/lib/sample-sales.json` | **Sí** (fuente por defecto) |
| Backend (.NET 10) | local (docker) u opcionalmente Render · `backend/` | No (opcional) |
| Mock (nginx) | local (docker) u opcionalmente Render · `backend/mocks/sap/` | No (opcional) |
