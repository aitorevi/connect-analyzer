"use client";

import { formatAmountFull } from "../lib/format";

type TooltipEntry = { name?: string; value?: number | string };
type Props = {
  active?: boolean;
  payload?: TooltipEntry[];
  label?: string | number;
  labelFormatter?: (label: string | number) => string;
};

// Shared custom tooltip so every chart (bar, area, donut) renders the same way and picks
// up the theme via CSS variables. Recharts injects active/payload/label at runtime.
export default function ChartTooltip({
  active,
  payload,
  label,
  labelFormatter,
}: Props) {
  if (!active || !payload || payload.length === 0) return null;

  const entry = payload[0];
  const title =
    label != null
      ? labelFormatter
        ? labelFormatter(label)
        : String(label)
      : (entry.name ?? "");

  return (
    <div className="chart-tooltip">
      {title && <span className="chart-tooltip__label">{title}</span>}
      <span className="chart-tooltip__value">
        {formatAmountFull(Number(entry.value ?? 0))}
      </span>
    </div>
  );
}
