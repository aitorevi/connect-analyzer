"use client";

import { formatAmountFull } from "../lib/format";

type TooltipEntry = { name?: string; value?: number | string };
type Props = {
  active?: boolean;
  payload?: TooltipEntry[];
  label?: string | number;
  labelFormatter?: (label: string | number) => string;
};

// Shared custom tooltip. Handles single-series charts (bar/area/donut) and multi-series
// ones (the composed revenue+units chart), themed via CSS variables.
export default function ChartTooltip({
  active,
  payload,
  label,
  labelFormatter,
}: Props) {
  if (!active || !payload || payload.length === 0) return null;

  const title =
    label != null
      ? labelFormatter
        ? labelFormatter(label)
        : String(label)
      : payload.length === 1
        ? (payload[0].name ?? "")
        : "";

  const multi = payload.length > 1;

  return (
    <div className="chart-tooltip">
      {title && <span className="chart-tooltip__label">{title}</span>}
      {payload.map((entry, index) => (
        <span className="chart-tooltip__value" key={entry.name ?? index}>
          {multi && <span className="chart-tooltip__name">{entry.name}: </span>}
          {formatAmountFull(Number(entry.value ?? 0))}
        </span>
      ))}
    </div>
  );
}
