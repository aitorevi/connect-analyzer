import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import Dashboard from "./Dashboard";
import type { Sale } from "../lib/dashboard";

vi.mock("./ProductRevenueUnitsChart", () => ({
  default: ({ data }: { data: unknown[] }) => (
    <div data-testid="by-product">{data.length}</div>
  ),
}));
vi.mock("./ByCustomerChart", () => ({
  default: ({ data }: { data: unknown[] }) => (
    <div data-testid="by-customer">{data.length}</div>
  ),
}));
vi.mock("./RevenueOverTimeChart", () => ({
  default: ({ data }: { data: unknown[] }) => (
    <div data-testid="over-time">{data.length}</div>
  ),
}));

const sale = (productName: string, customerId: string): Sale => ({
  date: "2026-01-01",
  customerId,
  productName,
  quantity: 1,
  amount: 10,
});

describe("Dashboard", () => {
  it("derives the charts from the sales it receives", () => {
    render(<Dashboard sales={[sale("A", "C1"), sale("B", "C2")]} />);

    expect(screen.getByTestId("by-product")).toHaveTextContent("2");
    expect(screen.getByTestId("by-customer")).toHaveTextContent("2");
  });

  it("shows the empty-filter state when the date range excludes all sales", () => {
    render(<Dashboard sales={[sale("A", "C1")]} />);

    fireEvent.change(screen.getByLabelText("Desde"), {
      target: { value: "2027-01-01" },
    });

    expect(
      screen.getByText(/No hay ventas para los filtros seleccionados/i),
    ).toBeInTheDocument();
  });
});
