"use client";

import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { formatAmountCompact, formatDateShort } from "../lib/format";
import { useChartTheme } from "../lib/theme";
import type { DailyRevenue } from "../lib/analytics";
import ChartTooltip from "./ChartTooltip";

type Props = { data: DailyRevenue[] };

export default function RevenueOverTimeChart({ data }: Props) {
  const theme = useChartTheme();

  if (data.length === 0) {
    return (
      <p data-testid="empty-over-time" className="empty-state">
        No hay datos para mostrar.
      </p>
    );
  }

  return (
    <div role="img" aria-label="Gráfico de área: ingresos por fecha">
      <ResponsiveContainer width="100%" height={300}>
        <AreaChart data={data} margin={{ top: 10, right: 16, bottom: 8, left: 4 }}>
          <defs>
            <linearGradient id="revenueGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor={theme.series[0]} stopOpacity={0.45} />
              <stop offset="100%" stopColor={theme.series[0]} stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" stroke={theme.grid} vertical={false} />
          <XAxis
            dataKey="date"
            tickFormatter={formatDateShort}
            tick={{ fill: theme.axis, fontSize: 12 }}
            stroke={theme.border}
            minTickGap={24}
          />
          <YAxis
            tickFormatter={formatAmountCompact}
            tick={{ fill: theme.axis, fontSize: 12 }}
            stroke={theme.border}
            width={48}
          />
          <Tooltip
            content={<ChartTooltip labelFormatter={(l) => formatDateShort(String(l))} />}
          />
          <Area
            type="monotone"
            dataKey="total"
            name="Ingresos"
            stroke={theme.series[0]}
            strokeWidth={2}
            fill="url(#revenueGradient)"
            isAnimationActive={false}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
