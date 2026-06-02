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

export const formatInt = (value: number): string => intFormatter.format(value);

const dateFormatter = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
});

export const formatDateShort = (iso: string): string => {
  const [year, month, day] = iso.split("-").map(Number);
  if (!year || !month || !day) return iso;
  return dateFormatter.format(new Date(year, month - 1, day));
};
