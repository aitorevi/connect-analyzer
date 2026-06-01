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

const intFormatter = new Intl.NumberFormat("en", { maximumFractionDigits: 0 });

// Thousand-separated integers for counts (units sold, number of sales).
export const formatInt = (value: number): string => intFormatter.format(value);

const dateFormatter = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
});

// Short axis label for an ISO date ("2026-01-02" -> "Jan 2"). Parsed in local time
// from the Y-M-D parts so it never shifts a day across timezones.
export const formatDateShort = (iso: string): string => {
  const [year, month, day] = iso.split("-").map(Number);
  if (!year || !month || !day) return iso;
  return dateFormatter.format(new Date(year, month - 1, day));
};
