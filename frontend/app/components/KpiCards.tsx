import type { Kpis } from "../lib/analytics";
import { formatAmountFull, formatInt } from "../lib/format";

type Props = { kpis: Kpis };

// Headline metrics row. Presentational: receives the precomputed KPIs.
export default function KpiCards({ kpis }: Props) {
  const items: { label: string; value: string }[] = [
    { label: "Total revenue", value: formatAmountFull(kpis.totalRevenue) },
    { label: "Sales", value: formatInt(kpis.transactions) },
    { label: "Avg ticket", value: formatAmountFull(kpis.avgTicket) },
    { label: "Units sold", value: formatInt(kpis.totalUnits) },
    { label: "Top product", value: kpis.topProduct ?? "—" },
  ];

  return (
    <section className="kpi-grid" aria-label="Key metrics">
      {items.map((item) => (
        <div className="kpi card" key={item.label}>
          <span className="kpi__value">{item.value}</span>
          <span className="kpi__label">{item.label}</span>
        </div>
      ))}
    </section>
  );
}
