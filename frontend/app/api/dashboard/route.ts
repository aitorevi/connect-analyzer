import { NextResponse } from "next/server";
import { fetchDashboard, triggerRefresh } from "../../lib/dashboard";

// Same-origin proxy so the client can poll/refresh the backend without exposing BACKEND_URL
// to the browser or needing CORS (the backend fetch happens here, on the server).
export const dynamic = "force-dynamic";

// GET → current aggregates (empty arrays if the backend is still cold).
export async function GET() {
  return NextResponse.json(await fetchDashboard());
}

// POST → ask the backend to re-ingest from its configured source (self-heal a cold demo).
export async function POST() {
  return NextResponse.json({ ok: await triggerRefresh() });
}
