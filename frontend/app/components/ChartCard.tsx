import type { ReactNode } from "react";

type Props = {
  title: string;
  subtitle?: string;
  children: ReactNode;
};

// Card shell with a header, shared by every chart panel so the layout stays consistent.
export default function ChartCard({ title, subtitle, children }: Props) {
  return (
    <section className="card chart-card">
      <header className="chart-card__header">
        <h2 className="chart-card__title">{title}</h2>
        {subtitle && <p className="chart-card__subtitle">{subtitle}</p>}
      </header>
      {children}
    </section>
  );
}
