import type { CustomerTotal, ProductTotal, Sale } from "./dashboard";

export type DailyRevenue = { date: string; total: number };

export type Kpis = {
  totalRevenue: number;
  totalUnits: number;
  transactions: number;
  avgTicket: number;
  topProduct: string | null;
  topCustomer: string | null;
};

// Revenue aggregated by day, sorted ascending — the backend has no time aggregation,
// so we derive the series client-side from the raw sales (which carry the date).
export function revenueByDate(sales: Sale[]): DailyRevenue[] {
  const totals = new Map<string, number>();
  for (const sale of sales) {
    totals.set(sale.date, (totals.get(sale.date) ?? 0) + sale.amount);
  }
  return [...totals.entries()]
    .map(([date, total]) => ({ date, total }))
    .sort((a, b) => a.date.localeCompare(b.date));
}

// Headline numbers for the KPI row. byProduct/byCustomer arrive already sorted desc
// from the backend, so their first element is the top performer.
export function computeKpis(
  sales: Sale[],
  byProduct: ProductTotal[],
  byCustomer: CustomerTotal[],
): Kpis {
  const totalRevenue = sales.reduce((sum, s) => sum + s.amount, 0);
  const totalUnits = sales.reduce((sum, s) => sum + s.quantity, 0);
  const transactions = sales.length;
  return {
    totalRevenue,
    totalUnits,
    transactions,
    avgTicket: transactions > 0 ? totalRevenue / transactions : 0,
    topProduct: byProduct[0]?.product ?? null,
    topCustomer: byCustomer[0]?.customerId ?? null,
  };
}
