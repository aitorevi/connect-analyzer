"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import ProductRevenueUnitsChart from "./ProductRevenueUnitsChart";
import ByCustomerChart from "./ByCustomerChart";
import RevenueOverTimeChart from "./RevenueOverTimeChart";
import KpiCards from "./KpiCards";
import ChartCard from "./ChartCard";
import FilterBar from "./FilterBar";
import {
  EMPTY_FILTERS,
  computeKpis,
  customerTotals,
  dateRange,
  filterSales,
  hasActiveFilters,
  productRevenueUnits,
  productTotals,
  revenueByDate,
  salesCountByDate,
  uniqueCustomers,
  uniqueProducts,
  type Filters,
} from "../lib/analytics";
import type { DashboardData, Sale } from "../lib/dashboard";

type Props = { initialSales: Sale[] };

const POLL_INTERVAL_MS = 5000;
const MAX_ATTEMPTS = 30;

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

export default function Dashboard({ initialSales }: Props) {
  const initialEmpty = initialSales.length === 0;

  const [sales, setSales] = useState(initialSales);
  const [filters, setFilters] = useState<Filters>(EMPTY_FILTERS);
  const [warming, setWarming] = useState(initialEmpty);
  const [gaveUp, setGaveUp] = useState(false);
  const running = useRef(false);

  const hasData = sales.length > 0;
  const active = hasActiveFilters(filters);

  const range = useMemo(() => dateRange(sales), [sales]);
  const productOptions = useMemo(() => uniqueProducts(sales), [sales]);
  const customerOptions = useMemo(() => uniqueCustomers(sales), [sales]);

  const filtered = useMemo(() => filterSales(sales, filters), [sales, filters]);
  const byProduct = useMemo(() => productTotals(filtered), [filtered]);
  const byCustomer = useMemo(() => customerTotals(filtered), [filtered]);
  const kpis = useMemo(
    () => computeKpis(filtered, byProduct, byCustomer),
    [filtered, byProduct, byCustomer],
  );
  const overTime = useMemo(() => revenueByDate(filtered), [filtered]);
  const salesCount = useMemo(() => salesCountByDate(filtered), [filtered]);
  const productData = useMemo(
    () => productRevenueUnits(byProduct, filtered),
    [byProduct, filtered],
  );

  const poll = useCallback(async () => {
    if (running.current) return;
    running.current = true;

    for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
      try {
        await fetch("/api/dashboard", { method: "POST" });
      } catch {
      }
      try {
        const res = await fetch("/api/dashboard", { cache: "no-store" });
        if (res.ok) {
          const data: DashboardData = await res.json();
          if (data.sales.length > 0) {
            setSales(data.sales);
            setWarming(false);
            running.current = false;
            return;
          }
        }
      } catch {
      }
      await delay(POLL_INTERVAL_MS);
    }

    setWarming(false);
    setGaveUp(true);
    running.current = false;
  }, []);

  useEffect(() => {
    if (!initialEmpty) return;
    const id = setTimeout(poll, 0);
    return () => clearTimeout(id);
  }, [initialEmpty, poll]);

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
          <button type="button" onClick={() => { setGaveUp(false); setWarming(true); poll(); }}>
            Reintentar
          </button>
        </p>
      )}

      {hasData && (
        <FilterBar
          filters={filters}
          onChange={setFilters}
          onReset={() => setFilters(EMPTY_FILTERS)}
          range={range}
          productOptions={productOptions}
          customerOptions={customerOptions}
          active={active}
        />
      )}

      {hasData && filtered.length === 0 && (
        <p className="subtitle" role="status">
          No hay ventas para los filtros seleccionados.{" "}
          <button type="button" onClick={() => setFilters(EMPTY_FILTERS)}>
            Limpiar filtros
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
