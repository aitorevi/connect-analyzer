import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import Dashboard from "./Dashboard";
import type { Sale } from "../lib/dashboard";

// Stub the charts so the test doesn't depend on Recharts/ResponsiveContainer layout.
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
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("derives the charts from sales and never warms up when data is present", () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch");

    render(<Dashboard initialSales={[sale("A", "C1"), sale("B", "C2")]} />);

    // Two distinct products / customers derived from the sales.
    expect(screen.getByTestId("by-product")).toHaveTextContent("2");
    expect(screen.getByTestId("by-customer")).toHaveTextContent("2");
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
    expect(fetchSpy).not.toHaveBeenCalled();
  });

  it("shows a warming message and triggers a refresh when starting empty", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({ sales: [] }),
    } as Response);

    render(<Dashboard initialSales={[]} />);

    expect(await screen.findByRole("status")).toHaveTextContent(
      /Calentando la demo/i,
    );
    await waitFor(() =>
      expect(fetchSpy).toHaveBeenCalledWith("/api/dashboard", { method: "POST" }),
    );
  });
});
