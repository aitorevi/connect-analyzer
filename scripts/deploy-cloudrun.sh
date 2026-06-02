#!/usr/bin/env bash
# Despliega backend + mock en Google Cloud Run (free tier, sin cold-start largo).
# Referencia/recordatorio: lo normal es ir paso a paso (ver DEPLOY.md), pero este script
# automatiza los dos `gcloud run deploy` y deja conectado SapMock__BaseUrl.
#
# Requisitos previos (una vez):
#   gcloud auth login
#   gcloud config set project <TU_PROJECT_ID>
#   gcloud services enable run.googleapis.com cloudbuild.googleapis.com artifactregistry.googleapis.com
#
# Uso:  ./scripts/deploy-cloudrun.sh
set -euo pipefail

REGION="${REGION:-europe-southwest1}"   # Madrid; cámbialo si quieres otra región
MOCK_SERVICE="connect-analyzer-mock"
API_SERVICE="connect-analyzer-api"

echo "▶ Desplegando mock ($MOCK_SERVICE) desde backend/mocks/sap…"
gcloud run deploy "$MOCK_SERVICE" \
  --source backend/mocks/sap \
  --region "$REGION" \
  --port 8080 \
  --allow-unauthenticated

MOCK_URL="$(gcloud run services describe "$MOCK_SERVICE" --region "$REGION" --format='value(status.url)')"
echo "✔ Mock en: $MOCK_URL"

echo "▶ Desplegando backend ($API_SERVICE) desde backend/…"
gcloud run deploy "$API_SERVICE" \
  --source backend \
  --region "$REGION" \
  --port 8080 \
  --allow-unauthenticated \
  --set-env-vars "SalesSource=Mock,SapMock__BaseUrl=${MOCK_URL},Sqlite__Path=/tmp/sales.db"

API_URL="$(gcloud run services describe "$API_SERVICE" --region "$REGION" --format='value(status.url)')"
echo "✔ Backend en: $API_URL"
echo
echo "Siguiente paso: en Vercel, pon BACKEND_URL=$API_URL y haz Redeploy."
