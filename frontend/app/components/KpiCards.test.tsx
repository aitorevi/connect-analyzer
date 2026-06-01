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
        }}
      />,
    );

    expect(screen.getByText("Total revenue")).toBeInTheDocument();
    expect(screen.getByText("1,234.5")).toBeInTheDocument();
    expect(screen.getByText("42")).toBeInTheDocument();
    expect(screen.getByText("Café Molido")).toBeInTheDocument();
  });
});
