#!/usr/bin/env bash
# One-time setup so GitHub Actions can deploy to Cloud Run keylessly (Workload Identity
# Federation). Creates a deployer service account + roles, a WIF pool/provider restricted to
# this repo, the impersonation binding, and the GitHub repo variables the workflow needs.
# Idempotent: safe to re-run. Requires gcloud (authed) and gh (authed with repo admin).
#
# Run once:  bash scripts/setup-cloudrun-cicd.sh
set -uo pipefail

PROJECT="connect-analyzer-demo"
PROJECT_NUMBER="370913301749"
REGION="europe-southwest1"
REPO="aitorevi/connect-analyzer"
POOL="github"
PROVIDER="github-provider"
SA="github-deployer@${PROJECT}.iam.gserviceaccount.com"

echo "▶ Enabling APIs…"
gcloud services enable \
  iamcredentials.googleapis.com sts.googleapis.com \
  run.googleapis.com cloudbuild.googleapis.com artifactregistry.googleapis.com \
  --project="$PROJECT"

echo "▶ Deployer service account…"
gcloud iam service-accounts create github-deployer --project="$PROJECT" \
  --display-name="GitHub Actions deployer" 2>/dev/null || echo "  (already exists)"

echo "▶ Roles…"
for ROLE in \
  roles/run.admin \
  roles/cloudbuild.builds.editor \
  roles/artifactregistry.admin \
  roles/storage.admin \
  roles/iam.serviceAccountUser
do
  gcloud projects add-iam-policy-binding "$PROJECT" \
    --member="serviceAccount:${SA}" --role="$ROLE" --condition=None >/dev/null \
    && echo "  + $ROLE"
done

echo "▶ Workload Identity pool + provider (restricted to ${REPO})…"
gcloud iam workload-identity-pools create "$POOL" --project="$PROJECT" \
  --location=global --display-name="GitHub" 2>/dev/null || echo "  (pool already exists)"

gcloud iam workload-identity-pools providers create-oidc "$PROVIDER" \
  --project="$PROJECT" --location=global --workload-identity-pool="$POOL" \
  --display-name="GitHub provider" \
  --issuer-uri="https://token.actions.githubusercontent.com" \
  --attribute-mapping="google.subject=assertion.sub,attribute.repository=assertion.repository" \
  --attribute-condition="assertion.repository=='${REPO}'" 2>/dev/null \
  || echo "  (provider already exists)"

echo "▶ Allow the repo to impersonate the deployer SA…"
gcloud iam service-accounts add-iam-policy-binding "$SA" --project="$PROJECT" \
  --role=roles/iam.workloadIdentityUser \
  --member="principalSet://iam.googleapis.com/projects/${PROJECT_NUMBER}/locations/global/workloadIdentityPools/${POOL}/attribute.repository/${REPO}"

WIF_PROVIDER="$(gcloud iam workload-identity-pools providers describe "$PROVIDER" \
  --project="$PROJECT" --location=global --workload-identity-pool="$POOL" \
  --format='value(name)')"

echo "▶ Setting GitHub repo variables…"
gh variable set GCP_PROJECT      --repo "$REPO" --body "$PROJECT"
gh variable set GCP_REGION       --repo "$REPO" --body "$REGION"
gh variable set GCP_DEPLOY_SA    --repo "$REPO" --body "$SA"
gh variable set GCP_WIF_PROVIDER --repo "$REPO" --body "$WIF_PROVIDER"

echo
echo "✅ Done."
echo "   GCP_WIF_PROVIDER = $WIF_PROVIDER"
echo "   GCP_DEPLOY_SA    = $SA"
