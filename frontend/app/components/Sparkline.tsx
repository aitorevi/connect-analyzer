"use client";

type Props = {
  values: number[];
  // A CSS colour (var() works here — it's the `color` property, not an SVG attribute);
  // the shapes use currentColor so they follow the theme.
  color?: string;
  height?: number;
};

// Tiny dependency-free trend line for KPI cards. Pure SVG, deterministic, no measurement.
export default function Sparkline({
  values,
  color = "var(--accent)",
  height = 32,
}: Props) {
  if (values.length < 2) return null;

  const width = 100;
  const max = Math.max(...values);
  const min = Math.min(...values);
  const range = max - min || 1;
  const stepX = width / (values.length - 1);

  const points = values
    .map((v, i) => {
      const x = (i * stepX).toFixed(2);
      const y = (height - ((v - min) / range) * height).toFixed(2);
      return `${x},${y}`;
    })
    .join(" ");

  return (
    <svg
      className="sparkline"
      viewBox={`0 0 ${width} ${height}`}
      preserveAspectRatio="none"
      role="presentation"
      style={{ color }}
    >
      <polygon points={`0,${height} ${points} ${width},${height}`} fill="currentColor" fillOpacity={0.12} />
      <polyline
        points={points}
        fill="none"
        stroke="currentColor"
        strokeWidth={1.5}
        strokeLinejoin="round"
        strokeLinecap="round"
        vectorEffect="non-scaling-stroke"
      />
    </svg>
  );
}
