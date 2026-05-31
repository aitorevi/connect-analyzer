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
  data: { customerId: string; totalAmount: number }[];
};

export default function ByCustomerChart({ data }: Props) {
  if (data.length === 0) {
    return (
      <p data-testid="empty-by-customer" className="empty-state">
        No hay datos para mostrar.
      </p>
    );
  }
  return (
    <ResponsiveContainer width="100%" height={320}>
      <BarChart data={data} margin={{ top: 10, right: 20, bottom: 20, left: 12 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="customerId" />
        <YAxis tickFormatter={formatAmountCompact} />
        <Tooltip formatter={(v: number) => formatAmountFull(v)} />
        <Bar dataKey="totalAmount" name="Amount" fill="#0ea5e9" />
      </BarChart>
    </ResponsiveContainer>
  );
}
