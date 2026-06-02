import { NextResponse } from "next/server";
import { fetchDashboard, triggerRefresh } from "../../lib/dashboard";

export const dynamic = "force-dynamic";

export async function GET() {
  return NextResponse.json(await fetchDashboard());
}

export async function POST() {
  return NextResponse.json({ ok: await triggerRefresh() });
}
