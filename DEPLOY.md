# Deploy

End-to-end deployment of the live demo: **backend + mock on [Render](https://render.com)** (Free tier,
no credit card required), **frontend on [Vercel](https://vercel.com)**. Since the frontend (`page.tsx`)
fetches the backend server-side, the demo flow is `Vercel (Next.js Server Component) → Render (backend)
→ Render (mock)` and **no browser CORS is involved**.

## Prerequisites

- A Render account (sign in with GitHub, no card needed for the Free plan).
- A Vercel account (same).
- (Optional, only for real SAP data) A free API key from the
  [SAP Business Accelerator Hub](https://api.sap.com).

## 1. Deploy backend + mock with one Render Blueprint

The repo ships a [`render.yaml`](./render.yaml) Blueprint that declares both Web Services.

1. Go to <https://dashboard.render.com> → **New +** → **Blueprint**.
2. Connect your GitHub account and pick the `aitorevi/connect-analyzer` repository.
3. Render reads `render.yaml`, shows the two services it is about to create
   (`connect-analyzer-mock` and `connect-analyzer-api`, Free plan, Frankfurt) → **Apply**.
4. The first build takes ~5 min per service. Watch the logs from each service page.

Render assigns:
- Mock: `https://connect-analyzer-mock.onrender.com` (or a suffix if that name is taken).
- API:  `https://connect-analyzer-api.onrender.com`

> If Render had to suffix the mock name, edit `SapMock__BaseUrl` in the **connect-analyzer-api**
> service's environment to the actual mock hostname, then **Manual Deploy** → **Clear build cache & deploy**.

### Smoke-test

```bash
curl -s https://connect-analyzer-mock.onrender.com/sales.txt | head -3
curl -s https://connect-analyzer-api.onrender.com/api/sales | head
curl -X POST https://connect-analyzer-api.onrender.com/api/sales/refresh   # forces re-ingestion
```

The first call may take 30-50 s if the service was sleeping (Free Web Services nap after ~15 min
without traffic).

### Optional: switch to real SAP data

By default the backend uses the mock (`SalesSource=Mock`). To pull from the real SAP S/4HANA OData
sandbox instead, in the **connect-analyzer-api** service:

1. **Environment** tab → set `Sap__ApiKey` (Secret) to your Business Accelerator Hub key.
2. Change `SalesSource` to `Sap`.
3. **Manual Deploy** → **Deploy latest commit**.

> The `Sap__ApiKey` variable is declared in `render.yaml` with `sync: false`, so Render does NOT
> populate it from the Blueprint — you must set it manually in the dashboard. Never commit the key.

## 2. Deploy the frontend (Vercel)

In the Vercel dashboard: **Add New… → Project → Import the GitHub repo `aitorevi/connect-analyzer`**.

- **Root Directory**: `frontend` (the Next.js app lives in this subfolder).
- **Framework Preset**: Next.js (auto-detected).
- **Environment Variables** — `BACKEND_URL` is **optional**:
  - **Leave it unset** (recommended) → the dashboard serves its bundled sample data
    (`frontend/app/lib/sample-sales.json`): instant, always-on, no backend needed. See [`DEMO.md`](./DEMO.md).
  - Or set `BACKEND_URL` = your backend URL (e.g. `https://connect-analyzer-api.onrender.com`) to
    use live data; the frontend falls back to the bundled data if it's unreachable.

Click **Deploy**. Vercel builds with `next build` and serves the dashboard.

> The backend (steps 1) is **not required** for the demo — the frontend is self-sufficient. Deploy
> it only if you want live data (incl. real SAP/Shopify) behind the dashboard.

## 3. Optional: lock CORS to your Vercel origin

`page.tsx` fetches the backend **server-side**, so the browser never calls the backend directly — CORS
is not exercised today. If you later add a client-side fetch, restrict the backend's CORS origin in the
**connect-analyzer-api** service environment:

```
Cors__AllowedOrigins__0 = https://<your-frontend>.vercel.app
```

Never widen to `AllowAnyOrigin`.

## Cold-start note

The free Render Web Services sleep after ~15 min of inactivity. The first request after that
cold-starts in 30-50 s. Two ways to mitigate if it matters for the demo:

- Visit the URL yourself a few seconds before showing it to someone (the dashboard `page.tsx` does
  two fetches that warm both backend and mock in one go).
- Set up a tiny cron pinger (UptimeRobot, cron-job.org, etc.) hitting `/api/sales` every ~10 min.
  Still on the free plan; just gentle keep-alive traffic.
