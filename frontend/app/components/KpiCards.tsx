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

export default function KpiCards({ kpis, revenueTrend, salesTrend }: Props) {
  const items: Item[] = [
    {
      label: "Ingresos totales",
      value: formatAmountFull(kpis.totalRevenue),
      trend: revenueTrend,
      color: "var(--chart-1)",
    },
    {
      label: "Ventas",
      value: formatInt(kpis.transactions),
      trend: salesTrend,
      color: "var(--chart-2)",
    },
    { label: "Ticket medio", value: formatAmountFull(kpis.avgTicket) },
    { label: "Unidades", value: formatInt(kpis.totalUnits) },
    { label: "Clientes", value: formatInt(kpis.distinctCustomers) },
    { label: "Productos", value: formatInt(kpis.distinctProducts) },
    {
      label: "Mejor día",
      value: kpis.bestDayDate ? formatAmountFull(kpis.bestDayTotal) : "—",
      sub: kpis.bestDayDate ? formatDateShort(kpis.bestDayDate) : undefined,
    },
  ];

  return (
    <section className="kpi-grid" aria-label="Métricas clave">
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
