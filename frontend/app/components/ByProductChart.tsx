"use client";

import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { formatAmountCompact } from "../lib/format";
import { useChartTheme } from "../lib/theme";
import ChartTooltip from "./ChartTooltip";

type Props = {
  data: { product: string; totalAmount: number }[];
};

export default function ByProductChart({ data }: Props) {
  const theme = useChartTheme();

  if (data.length === 0) {
    return (
      <p data-testid="empty-by-product" className="empty-state">
        No hay datos para mostrar.
      </p>
    );
  }
  return (
    <ResponsiveContainer width="100%" height={320}>
      <BarChart data={data} margin={{ top: 10, right: 16, bottom: 28, left: 4 }}>
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
          tickFormatter={formatAmountCompact}
          tick={{ fill: theme.axis, fontSize: 12 }}
          stroke={theme.border}
          width={48}
        />
        <Tooltip cursor={{ fill: theme.cursor }} content={<ChartTooltip />} />
        <Bar
          dataKey="totalAmount"
          name="Amount"
          fill={theme.series[1]}
          radius={[6, 6, 0, 0]}
          maxBarSize={56}
          isAnimationActive={false}
        />
      </BarChart>
    </ResponsiveContainer>
  );
}
