import sampleSales from "./sample-sales.json";

export type ProductTotal = { product: string; totalAmount: number };
export type CustomerTotal = { customerId: string; totalAmount: number };
export type Sale = {
  date: string;
  customerId: string;
  productName: string;
  quantity: number;
  amount: number;
};

export type DashboardData = {
  sales: Sale[];
};

const SAMPLE: DashboardData = { sales: sampleSales as Sale[] };

const backendUrl = () => process.env.BACKEND_URL ?? "http://localhost:5080";

export async function fetchDashboard(timeoutMs = 6000): Promise<DashboardData> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const res = await fetch(`${backendUrl()}/api/sales`, {
      cache: "no-store",
      signal: controller.signal,
    });
    if (!res.ok) return SAMPLE;
    const sales: Sale[] = await res.json();
    return sales.length > 0 ? { sales } : SAMPLE;
  } catch {
    return SAMPLE;
  } finally {
    clearTimeout(timer);
  }
}

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
