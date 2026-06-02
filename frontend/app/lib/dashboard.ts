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

const backendUrl = () => process.env.BACKEND_URL ?? "http://localhost:5080";

export async function fetchDashboard(): Promise<DashboardData> {
  const res = await fetch(`${backendUrl()}/api/sales`, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Backend responded ${res.status} on /api/sales`);
  }
  return { sales: await res.json() };
}
