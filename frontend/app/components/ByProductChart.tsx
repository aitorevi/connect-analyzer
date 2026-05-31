"use client";

import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import { formatAmountCompact, formatAmountFull } from "../lib/format";

type Props = {
  data: { product: string; totalAmount: number }[];
};

export default function ByProductChart({ data }: Props) {
  if (data.length === 0) {
    return (
      <p data-testid="empty-by-product" className="empty-state">
        No hay datos para mostrar.
      </p>
    );
  }
  return (
    <ResponsiveContainer width="100%" height={320}>
      <BarChart data={data} margin={{ top: 10, right: 20, bottom: 20, left: 12 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="product" />
        <YAxis tickFormatter={formatAmountCompact} />
        <Tooltip formatter={(v: number) => formatAmountFull(v)} />
        <Bar dataKey="totalAmount" name="Amount" fill="#4f46e5" />
      </BarChart>
    </ResponsiveContainer>
  );
}
