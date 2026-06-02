# Demo en vivo: cómo está montada

Referencia de cómo funciona la demo pública: qué hay desplegado, de dónde salen los datos y cómo
usar el backend. **Resumen:** frontend en **Vercel** → backend en **Google Cloud Run** → mock en
**Cloud Run**. El frontend hace fetch server-side, así que no hay CORS de navegador.

## Por qué Cloud Run (y no Render)

Render free dormía el backend y el mock (~15 min) y devolvía 502 al despertar, dejando la demo en
blanco. Lo intentamos tapar con workarounds en el frontend (auto-heal + datos embebidos), pero la
solución de raíz fue mover el backend a **Cloud Run**: también escala a cero, pero el arranque en
frío es ~1-2 s y **la petición espera** al contenedor en vez de fallar. Así la demo carga sin el
problema de "no hay datos". Se eliminaron los workarounds del frontend.

## Dónde está desplegado cada pieza

| Pieza | Hosting | Notas |
|-------|---------|-------|
| Frontend (Next.js) | **Vercel** · `frontend/` | Siempre activo. Auto-deploy en cada push a `main`. `connect-analyzer.vercel.app`. |
| Backend (.NET 10) | **Cloud Run** · `connect-analyzer-api` | Lee del mock, agrega y sirve la API REST. <https://connect-analyzer-api-370913301749.europe-southwest1.run.app> |
| Mock (nginx) | **Cloud Run** · `connect-analyzer-mock` | Sirve `sales.txt` (fixtures). <https://connect-analyzer-mock-370913301749.europe-southwest1.run.app> |

## Cómo coge los datos

1. **SSR**: `frontend/app/page.tsx` llama a `fetchDashboard()` (`frontend/app/lib/dashboard.ts`), que
   hace `GET <BACKEND_URL>/api/sales` en el servidor. Si el backend no responde, lanza y se muestra
   el error boundary (`app/error.tsx`).
2. **Cliente**: `Dashboard` recibe las ventas crudas y **deriva en cliente** KPIs, series, agregados
   por producto/cliente y aplica los **filtros** (`frontend/app/lib/analytics.ts`). Sin más llamadas
   al backend: los filtros recalculan sobre las ventas ya cargadas.
3. **`BACKEND_URL`** (env var de Vercel) apunta al servicio `connect-analyzer-api` de Cloud Run. En
   local por defecto es `http://localhost:5080`; en Docker Compose, `http://backend:8080`.

## El backend: cómo usarlo

### En local
```bash
docker compose up --build      # mock + backend + frontend (frontend en :3000)
```
Fuente de datos por **`SalesSource`** (ver `backend/CLAUDE.md` y `.env.example`):
- `Mock` (por defecto) — lee el `.txt` del servicio `sap-mock`.
- `Sap` — OData real del SAP Business Accelerator Hub (secreto `Sap__ApiKey`).
- `Shopify` — Admin API real (`Shopify__StoreUrl`, `Shopify__ClientId`, `Shopify__ClientSecret`).

Los secretos van en un `.env` en la raíz (gitignored), nunca en git.

### Desplegar / actualizar en Cloud Run
Ver [`DEPLOY.md`](./DEPLOY.md) (pasos `gcloud run deploy` o `scripts/deploy-cloudrun.sh`). Tras
desplegar, poner `BACKEND_URL` en Vercel con la URL del backend y redeploy.
