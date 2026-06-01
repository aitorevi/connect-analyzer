"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import ProductRevenueUnitsChart from "./ProductRevenueUnitsChart";
import ByCustomerChart from "./ByCustomerChart";
import RevenueOverTimeChart from "./RevenueOverTimeChart";
import KpiCards from "./KpiCards";
import ChartCard from "./ChartCard";
import {
  computeKpis,
  productRevenueUnits,
  revenueByDate,
  salesCountByDate,
} from "../lib/analytics";
import type {
  CustomerTotal,
  DashboardData,
  ProductTotal,
  Sale,
} from "../lib/dashboard";

type Props = {
  initialByProduct: ProductTotal[];
  initialByCustomer: CustomerTotal[];
  initialSales: Sale[];
};

const POLL_INTERVAL_MS = 5000;
const MAX_ATTEMPTS = 30; // ~2.5 min, covers a free-tier mock + backend cold start

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

// Wraps the dashboard and self-heals on a free-tier cold start: when we render with no data
// (the store is empty until the backend re-seeds from the sleeping mock) we re-trigger a
// refresh and poll until rows appear, instead of leaving a dead "No hay datos".
export default function Dashboard({
  initialByProduct,
  initialByCustomer,
  initialSales,
}: Props) {
  const initialEmpty =
    initialByProduct.length === 0 && initialByCustomer.length === 0;

  const [byProduct, setByProduct] = useState(initialByProduct);
  const [byCustomer, setByCustomer] = useState(initialByCustomer);
  const [sales, setSales] = useState(initialSales);
  const [warming, setWarming] = useState(initialEmpty);
  const [gaveUp, setGaveUp] = useState(false);
  const running = useRef(false);

  const hasData = byProduct.length > 0 || byCustomer.length > 0;

  const kpis = useMemo(
    () => computeKpis(sales, byProduct, byCustomer),
    [sales, byProduct, byCustomer],
  );
  const overTime = useMemo(() => revenueByDate(sales), [sales]);
  const salesCount = useMemo(() => salesCountByDate(sales), [sales]);
  const productData = useMemo(
    () => productRevenueUnits(byProduct, sales),
    [byProduct, sales],
  );

  // Re-triggers an ingestion each round, then checks for rows. On a cold free-tier stack the
  // first refresh 502s (the mock is asleep) but *wakes* it, so a later attempt succeeds — a
  // single refresh isn't enough. No synchronous state updates: every setState is post-await.
  const poll = useCallback(async () => {
    if (running.current) return;
    running.current = true;

    for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
      try {
        await fetch("/api/dashboard", { method: "POST" });
      } catch {
        // ignore — the GET below decides whether we have data
      }
      try {
        const res = await fetch("/api/dashboard", { cache: "no-store" });
        if (res.ok) {
          const data: DashboardData = await res.json();
          if (data.byProduct.length > 0 || data.byCustomer.length > 0) {
            setByProduct(data.byProduct);
            setByCustomer(data.byCustomer);
            setSales(data.sales);
            setWarming(false);
            running.current = false;
            return;
          }
        }
      } catch {
        // keep polling — a cold backend may still be waking up
      }
      await delay(POLL_INTERVAL_MS);
    }

    setWarming(false);
    setGaveUp(true);
    running.current = false;
  }, []);

  const retry = useCallback(() => {
    setGaveUp(false);
    setWarming(true);
    poll();
  }, [poll]);

  useEffect(() => {
    // Mount only: if SSR already delivered data we never warm up. Deferred so the async
    // work starts outside the effect body (no synchronous state update on mount).
    if (!initialEmpty) return;
    const id = setTimeout(poll, 0);
    return () => clearTimeout(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <>
      {warming && !hasData && (
        <p className="subtitle" role="status">
          Calentando la demo… el backend gratuito de Render tarda ~1 min en arrancar en frío.
          Reintentando solo.
        </p>
      )}
      {gaveUp && !hasData && (
        <p className="subtitle" role="alert">
          La demo sigue arrancando.{" "}
          <button type="button" onClick={retry}>
            Reintentar
          </button>
        </p>
      )}

      <KpiCards
        kpis={kpis}
        revenueTrend={overTime.map((d) => d.total)}
        salesTrend={salesCount.map((d) => d.count)}
      />

      <ChartCard title="Revenue over time">
        <RevenueOverTimeChart data={overTime} />
      </ChartCard>

      <div className="chart-grid">
        <ChartCard title="Revenue & units by product">
          <ProductRevenueUnitsChart data={productData} />
        </ChartCard>
        <ChartCard title="Total amount by customer">
          <ByCustomerChart data={byCustomer} />
        </ChartCard>
      </div>
    </>
  );
}
