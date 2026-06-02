# Deploy

End-to-end deployment of the live demo: **backend + mock on [Google Cloud Run](https://cloud.google.com/run)**
(free tier, no cold-start of the "no data" kind), **frontend on [Vercel](https://vercel.com)**. The
frontend (`page.tsx`) fetches the backend server-side, so the flow is
`Vercel (Next.js Server Component) → Cloud Run (backend) → Cloud Run (mock)` and **no browser CORS is
involved**.

> Why Cloud Run instead of Render: Render's free Web Services sleep after ~15 min and return 502 while
> waking, which left the demo blank. Cloud Run scales to zero too, but cold starts are ~1-2 s and the
> request **waits** for the container instead of failing — so the demo just works.

## Prerequisites

- The `gcloud` CLI installed and authenticated (`gcloud auth login`).
- A Google Cloud project with billing enabled (the Cloud Run free tier covers this demo).
- A Vercel account.
- (Optional, only for real SAP data) a free API key from the
  [SAP Business Accelerator Hub](https://api.sap.com).

## 1. Deploy backend + mock to Cloud Run

Both pieces ship a Dockerfile and listen on `8080`. `gcloud run deploy --source <dir>` builds the
image with Cloud Build and deploys it. Run the steps below (or use
[`scripts/deploy-cloudrun.sh`](./scripts/deploy-cloudrun.sh), which does the two deploys and wires
`SapMock__BaseUrl` for you).

```bash
# Once: select the project and enable the APIs
gcloud config set project <YOUR_PROJECT_ID>
gcloud services enable run.googleapis.com cloudbuild.googleapis.com artifactregistry.googleapis.com

# 1a. Mock (nginx serving the pipe-delimited Latin-1 .txt)
gcloud run deploy connect-analyzer-mock \
  --source backend/mocks/sap \
  --region europe-southwest1 \
  --port 8080 --allow-unauthenticated

# Grab its URL
MOCK_URL=$(gcloud run services describe connect-analyzer-mock \
  --region europe-southwest1 --format='value(status.url)')

# 1b. Backend (.NET 10), pointed at the mock, SQLite in /tmp (ephemeral, re-seeded on start)
gcloud run deploy connect-analyzer-api \
  --source backend \
  --region europe-southwest1 \
  --port 8080 --allow-unauthenticated \
  --set-env-vars "SalesSource=Mock,SapMock__BaseUrl=${MOCK_URL},Sqlite__Path=/tmp/sales.db"
```

Smoke-test (URLs are printed by the deploys / `gcloud run services describe`):

```bash
curl -s <mock-url>/sales.txt | head -3
curl -s <api-url>/api/sales | head
curl -s <api-url>/api/sales/by-product
```

### Optional: real SAP / Shopify instead of the mock

Set `SalesSource` and the matching secrets on the `connect-analyzer-api` service (then it redeploys):

```bash
# SAP
gcloud run services update connect-analyzer-api --region europe-southwest1 \
  --set-env-vars SalesSource=Sap --set-env-vars Sap__ApiKey=<your-key>
# Shopify
gcloud run services update connect-analyzer-api --region europe-southwest1 \
  --set-env-vars SalesSource=Shopify \
  --set-env-vars Shopify__StoreUrl=<url>,Shopify__ClientId=<id>,Shopify__ClientSecret=<secret>
```

(For real secrets, prefer Secret Manager + `--set-secrets` over `--set-env-vars`.)

## 2. Deploy the frontend (Vercel)

In the Vercel dashboard: **Add New… → Project → Import the GitHub repo `aitorevi/connect-analyzer`**.

- **Root Directory**: `frontend`.
- **Framework Preset**: Next.js (auto-detected).
- **Environment Variables**: `BACKEND_URL` = the `connect-analyzer-api` Cloud Run URL (from step 1b).

Click **Deploy**. The frontend fetches the backend server-side and renders the dashboard; the
client derives KPIs/series and applies the filters.

## 3. Optional: lock CORS to your Vercel origin

`page.tsx` fetches the backend **server-side**, so the browser never calls the backend directly —
CORS is not exercised today. If you later add a client-side fetch, restrict the backend's CORS origin
on the `connect-analyzer-api` service:

```
Cors__AllowedOrigins__0 = https://<your-frontend>.vercel.app
```

Never widen to `AllowAnyOrigin`.
