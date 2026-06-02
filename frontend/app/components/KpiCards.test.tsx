import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import KpiCards from "./KpiCards";

describe("KpiCards", () => {
  it("renders the formatted headline metrics", () => {
    render(
      <KpiCards
        kpis={{
          totalRevenue: 1234.5,
          totalUnits: 42,
          transactions: 10,
          avgTicket: 123.45,
          topProduct: "Café Molido",
          topCustomer: "C1",
          distinctCustomers: 5,
          distinctProducts: 6,
          bestDayDate: "2026-01-06",
          bestDayTotal: 257,
        }}
        revenueTrend={[10, 20, 15]}
        salesTrend={[1, 3, 2]}
      />,
    );

    expect(screen.getByText("Ingresos totales")).toBeInTheDocument();
    expect(screen.getByText("1,234.5")).toBeInTheDocument();
    expect(screen.getByText("42")).toBeInTheDocument();
    expect(screen.getByText("Clientes")).toBeInTheDocument();
    expect(screen.getByText("Productos")).toBeInTheDocument();
  });
});
