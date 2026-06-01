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

// --- Filtering & client-side aggregates -----------------------------------
// With filters, the backend's precomputed by-product/by-customer would be stale, so the
// dashboard derives every aggregate from the (filtered) raw sales instead.

export type Filters = {
  from: string | null; // inclusive ISO date, null = no lower bound
  to: string | null; // inclusive ISO date, null = no upper bound
  products: string[]; // empty = all products
  customers: string[]; // empty = all customers
};

export const EMPTY_FILTERS: Filters = {
  from: null,
  to: null,
  products: [],
  customers: [],
};

// ISO YYYY-MM-DD compares lexicographically the same as chronologically.
export function filterSales(sales: Sale[], filters: Filters): Sale[] {
  const products = filters.products.length > 0 ? new Set(filters.products) : null;
  const customers = filters.customers.length > 0 ? new Set(filters.customers) : null;
  return sales.filter(
    (s) =>
      (!filters.from || s.date >= filters.from) &&
      (!filters.to || s.date <= filters.to) &&
      (!products || products.has(s.productName)) &&
      (!customers || customers.has(s.customerId)),
  );
}

export function hasActiveFilters(filters: Filters): boolean {
  return (
    filters.from !== null ||
    filters.to !== null ||
    filters.products.length > 0 ||
    filters.customers.length > 0
  );
}

export function dateRange(sales: Sale[]): { min: string | null; max: string | null } {
  if (sales.length === 0) return { min: null, max: null };
  let min = sales[0].date;
  let max = sales[0].date;
  for (const s of sales) {
    if (s.date < min) min = s.date;
    if (s.date > max) max = s.date;
  }
  return { min, max };
}

export const uniqueProducts = (sales: Sale[]): string[] =>
  [...new Set(sales.map((s) => s.productName))].sort((a, b) => a.localeCompare(b));

export const uniqueCustomers = (sales: Sale[]): string[] =>
  [...new Set(sales.map((s) => s.customerId))].sort((a, b) => a.localeCompare(b));

// Client-side equivalents of the backend aggregates, sorted by amount desc.
export function productTotals(sales: Sale[]): ProductTotal[] {
  const totals = new Map<string, number>();
  for (const s of sales) {
    totals.set(s.productName, (totals.get(s.productName) ?? 0) + s.amount);
  }
  return [...totals.entries()]
    .map(([product, totalAmount]) => ({ product, totalAmount }))
    .sort((a, b) => b.totalAmount - a.totalAmount);
}

export function customerTotals(sales: Sale[]): CustomerTotal[] {
  const totals = new Map<string, number>();
  for (const s of sales) {
    totals.set(s.customerId, (totals.get(s.customerId) ?? 0) + s.amount);
  }
  return [...totals.entries()]
    .map(([customerId, totalAmount]) => ({ customerId, totalAmount }))
    .sort((a, b) => b.totalAmount - a.totalAmount);
}
