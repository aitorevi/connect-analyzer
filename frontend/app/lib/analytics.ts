import type { CustomerTotal, ProductTotal, Sale } from "./dashboard";

export type DailyRevenue = { date: string; total: number };
export type DailyCount = { date: string; count: number };
export type ProductRevenueUnits = {
  product: string;
  revenue: number;
  units: number;
};

export type Kpis = {
  totalRevenue: number;
  totalUnits: number;
  transactions: number;
  avgTicket: number;
  topProduct: string | null;
  topCustomer: string | null;
  distinctCustomers: number;
  distinctProducts: number;
  bestDayDate: string | null;
  bestDayTotal: number;
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

// Number of sales per day, sorted ascending — feeds the "Sales" sparkline.
export function salesCountByDate(sales: Sale[]): DailyCount[] {
  const counts = new Map<string, number>();
  for (const sale of sales) {
    counts.set(sale.date, (counts.get(sale.date) ?? 0) + 1);
  }
  return [...counts.entries()]
    .map(([date, count]) => ({ date, count }))
    .sort((a, b) => a.date.localeCompare(b.date));
}

// Revenue (from the backend aggregate) joined with units sold (summed from raw sales),
// preserving the backend's revenue-desc order. Powers the revenue+units composed chart.
export function productRevenueUnits(
  byProduct: ProductTotal[],
  sales: Sale[],
): ProductRevenueUnits[] {
  const units = new Map<string, number>();
  for (const sale of sales) {
    units.set(sale.productName, (units.get(sale.productName) ?? 0) + sale.quantity);
  }
  return byProduct.map((p) => ({
    product: p.product,
    revenue: p.totalAmount,
    units: units.get(p.product) ?? 0,
  }));
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

  const daily = revenueByDate(sales);
  const best = daily.reduce(
    (max, day) => (day.total > max.total ? day : max),
    { date: "", total: -Infinity },
  );

  return {
    totalRevenue,
    totalUnits,
    transactions,
    avgTicket: transactions > 0 ? totalRevenue / transactions : 0,
    topProduct: byProduct[0]?.product ?? null,
    topCustomer: byCustomer[0]?.customerId ?? null,
    distinctCustomers: new Set(sales.map((s) => s.customerId)).size,
    distinctProducts: new Set(sales.map((s) => s.productName)).size,
    bestDayDate: daily.length > 0 ? best.date : null,
    bestDayTotal: daily.length > 0 ? best.total : 0,
  };
}
