export type ProductTotal = { product: string; totalAmount: number };
export type CustomerTotal = { customerId: string; totalAmount: number };
export type DashboardData = {
  byProduct: ProductTotal[];
  byCustomer: CustomerTotal[];
};

const backendUrl = () => process.env.BACKEND_URL ?? "http://localhost:5080";

// Server-side fetch of both aggregates. Never throws: on a cold/unreachable backend it
// resolves to empty arrays so the page can render and the client can warm the demo up.
// The timeout keeps SSR from hanging on a sleeping free-tier backend.
export async function fetchDashboard(timeoutMs = 6000): Promise<DashboardData> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const base = backendUrl();
    const [product, customer] = await Promise.all([
      fetch(`${base}/api/sales/by-product`, { cache: "no-store", signal: controller.signal }),
      fetch(`${base}/api/sales/by-customer`, { cache: "no-store", signal: controller.signal }),
    ]);
    if (!product.ok || !customer.ok) return { byProduct: [], byCustomer: [] };
    return { byProduct: await product.json(), byCustomer: await customer.json() };
  } catch {
    return { byProduct: [], byCustomer: [] };
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
