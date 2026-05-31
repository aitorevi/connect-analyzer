// Compact numeric labels for chart axes (e.g. 800000 -> "800K", 1500000 -> "1.5M") and
// thousand-separated full numbers for tooltips (e.g. 649350 -> "649,350"). Centralised so
// every chart in the dashboard renders amounts the same way.

const compactFormatter = new Intl.NumberFormat("en", {
  notation: "compact",
  maximumFractionDigits: 1,
});

const fullFormatter = new Intl.NumberFormat("en", {
  maximumFractionDigits: 2,
});

export const formatAmountCompact = (value: number): string =>
  compactFormatter.format(value);

export const formatAmountFull = (value: number): string =>
  fullFormatter.format(value);
