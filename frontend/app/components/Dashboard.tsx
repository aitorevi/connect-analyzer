"use client";

import { useMemo, useState } from "react";
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
import type { Sale } from "../lib/dashboard";

type Props = { sales: Sale[] };

export default function Dashboard({ sales }: Props) {
  const [filters, setFilters] = useState<Filters>(EMPTY_FILTERS);

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

  return (
    <>
      <FilterBar
        filters={filters}
        onChange={setFilters}
        onReset={() => setFilters(EMPTY_FILTERS)}
        range={range}
        productOptions={productOptions}
        customerOptions={customerOptions}
        active={active}
      />

      {filtered.length === 0 && (
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

      <ChartCard title="Ingresos en el tiempo">
        <RevenueOverTimeChart data={overTime} />
      </ChartCard>

      <div className="chart-grid">
        <ChartCard title="Ingresos y unidades por producto">
          <ProductRevenueUnitsChart data={productData} />
        </ChartCard>
        <ChartCard title="Importe total por cliente">
          <ByCustomerChart data={byCustomer} />
        </ChartCard>
      </div>
    </>
  );
}
