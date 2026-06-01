"use client";

import {
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
} from "recharts";
import { useChartTheme } from "../lib/theme";
import ChartTooltip from "./ChartTooltip";

type Props = {
  data: { customerId: string; totalAmount: number }[];
};

export default function ByCustomerChart({ data }: Props) {
  const theme = useChartTheme();

  if (data.length === 0) {
    return (
      <p data-testid="empty-by-customer" className="empty-state">
        No hay datos para mostrar.
      </p>
    );
  }
  return (
    <ResponsiveContainer width="100%" height={320}>
      <PieChart>
        <Pie
          data={data}
          dataKey="totalAmount"
          nameKey="customerId"
          innerRadius={68}
          outerRadius={108}
          paddingAngle={2}
          stroke={theme.surface}
          strokeWidth={2}
          isAnimationActive={false}
        >
          {data.map((entry, index) => (
            <Cell
              key={entry.customerId}
              fill={theme.series[index % theme.series.length]}
            />
          ))}
        </Pie>
        <Tooltip content={<ChartTooltip />} />
        <Legend wrapperStyle={{ fontSize: 12, color: theme.axis }} />
      </PieChart>
    </ResponsiveContainer>
  );
}
