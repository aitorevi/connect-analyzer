export type ProductTotal = { product: string; totalAmount: number };
export type CustomerTotal = { customerId: string; totalAmount: number };
export type Sale = {
  date: string; // ISO "YYYY-MM-DD"
  customerId: string;
  productName: string;
  quantity: number;
  amount: number;
};
// The dashboard derives every aggregate client-side from the raw sales (so filters can
// recompute them), so a single fetch of the raw sales is all it needs.
export type DashboardData = {
  sales: Sale[];
};

const EMPTY: DashboardData = { sales: [] };

const backendUrl = () => process.env.BACKEND_URL ?? "http://localhost:5080";

// Server-side fetch of both aggregates. Never throws: on a cold/unreachable backend it
// resolves to empty arrays so the page can render and the client can warm the demo up.
// The timeout keeps SSR from hanging on a sleeping free-tier backend.
export async function fetchDashboard(timeoutMs = 6000): Promise<DashboardData> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const res = await fetch(`${backendUrl()}/api/sales`, {
      cache: "no-store",
      signal: controller.signal,
    });
    if (!res.ok) return EMPTY;
    return { sales: await res.json() };
  } catch {
    return EMPTY;
  } finally {
    clearTimeout(timer);
  }
}

// Triggers a re-ingestion on the backend. Used to self-heal the demo when the store is
// empty (free-tier cold start). Never throws; returns whether the refresh succeeded.
export async function triggerRefresh(timeoutMs = 90000): Promise<boolean> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const res = await fetch(`${backendUrl()}/api/sales/refresh`, {
      method: "POST",
      cache: "no-store",
      signal: controller.signal,
    });
    return res.ok;
  } catch {
    return false;
  } finally {
    clearTimeout(timer);
  }
}
