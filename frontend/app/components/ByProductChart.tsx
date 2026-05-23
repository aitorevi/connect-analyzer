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

type Props = {
  data: { product: string; totalAmount: number }[];
};

export default function ByProductChart({ data }: Props) {
  return (
    <ResponsiveContainer width="100%" height={320}>
      <BarChart data={data} margin={{ top: 10, right: 20, bottom: 20, left: 0 }}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="product" />
        <YAxis />
        <Tooltip formatter={(v: number) => v.toFixed(2)} />
        <Bar dataKey="totalAmount" name="Amount" fill="#4f46e5" />
      </BarChart>
    </ResponsiveContainer>
  );
}
