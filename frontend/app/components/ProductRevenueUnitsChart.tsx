"use client";

import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { formatAmountCompact, formatInt } from "../lib/format";
import { useChartTheme } from "../lib/theme";
import type { ProductRevenueUnits } from "../lib/analytics";
import ChartTooltip from "./ChartTooltip";

type Props = { data: ProductRevenueUnits[] };

export default function ProductRevenueUnitsChart({ data }: Props) {
  const theme = useChartTheme();

  if (data.length === 0) {
    return (
      <p data-testid="empty-product" className="empty-state">
        No hay datos para mostrar.
      </p>
    );
  }

  return (
    <div role="img" aria-label="Gráfico de barras y línea: ingresos y unidades por producto">
      <ResponsiveContainer width="100%" height={320}>
        <ComposedChart data={data} margin={{ top: 10, right: 8, bottom: 28, left: 4 }}>
          <CartesianGrid strokeDasharray="3 3" stroke={theme.grid} vertical={false} />
          <XAxis
            dataKey="product"
            tick={{ fill: theme.axis, fontSize: 12 }}
            stroke={theme.border}
            interval={0}
            angle={-15}
            textAnchor="end"
            height={56}
          />
          <YAxis
            yAxisId="revenue"
            tickFormatter={formatAmountCompact}
            tick={{ fill: theme.axis, fontSize: 12 }}
            stroke={theme.border}
            width={48}
          />
          <YAxis
            yAxisId="units"
            orientation="right"
            tickFormatter={formatInt}
            tick={{ fill: theme.axis, fontSize: 12 }}
            stroke={theme.border}
            width={40}
          />
          <Tooltip cursor={{ fill: theme.cursor }} content={<ChartTooltip />} />
          <Legend wrapperStyle={{ fontSize: 12, color: theme.axis }} />
          <Bar
            yAxisId="revenue"
            dataKey="revenue"
            name="Ingresos"
            fill={theme.series[1]}
            radius={[6, 6, 0, 0]}
            maxBarSize={48}
            isAnimationActive={false}
          />
          <Line
            yAxisId="units"
            dataKey="units"
            name="Unidades"
            stroke={theme.series[3]}
            strokeWidth={2}
            dot={{ r: 3 }}
            isAnimationActive={false}
          />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  );
}
