import type { Kpis } from "../lib/analytics";
import { formatAmountFull, formatDateShort, formatInt } from "../lib/format";
import Sparkline from "./Sparkline";

type Props = {
  kpis: Kpis;
  revenueTrend: number[];
  salesTrend: number[];
};

type Item = {
  label: string;
  value: string;
  sub?: string;
  trend?: number[];
  color?: string;
};

// Headline metrics row. Presentational: receives precomputed KPIs and the daily trends
// for the two cards that show a sparkline.
export default function KpiCards({ kpis, revenueTrend, salesTrend }: Props) {
  const items: Item[] = [
    {
      label: "Total revenue",
      value: formatAmountFull(kpis.totalRevenue),
      trend: revenueTrend,
      color: "var(--chart-1)",
    },
    {
      label: "Sales",
      value: formatInt(kpis.transactions),
      trend: salesTrend,
      color: "var(--chart-2)",
    },
    { label: "Avg ticket", value: formatAmountFull(kpis.avgTicket) },
    { label: "Units sold", value: formatInt(kpis.totalUnits) },
    { label: "Customers", value: formatInt(kpis.distinctCustomers) },
    { label: "Products", value: formatInt(kpis.distinctProducts) },
    {
      label: "Best day",
      value: kpis.bestDayDate ? formatAmountFull(kpis.bestDayTotal) : "—",
      sub: kpis.bestDayDate ? formatDateShort(kpis.bestDayDate) : undefined,
    },
  ];

  return (
    <section className="kpi-grid" aria-label="Key metrics">
      {items.map((item) => (
        <div className="kpi card" key={item.label}>
          <span className="kpi__value">{item.value}</span>
          <span className="kpi__label">
            {item.label}
            {item.sub && <span className="kpi__sub"> · {item.sub}</span>}
          </span>
          {item.trend && item.trend.length > 1 && (
            <Sparkline values={item.trend} color={item.color} />
          )}
        </div>
      ))}
    </section>
  );
}
