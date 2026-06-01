import { render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import Dashboard from "./Dashboard";

// Stub the charts so the test doesn't depend on Recharts/ResponsiveContainer layout.
vi.mock("./ByProductChart", () => ({
  default: ({ data }: { data: unknown[] }) => (
    <div data-testid="by-product">{data.length}</div>
  ),
}));
vi.mock("./ByCustomerChart", () => ({
  default: ({ data }: { data: unknown[] }) => (
    <div data-testid="by-customer">{data.length}</div>
  ),
}));

describe("Dashboard", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders the charts and never warms up when initial data is present", () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch");

    render(
      <Dashboard
        initialByProduct={[{ product: "A", totalAmount: 10 }]}
        initialByCustomer={[{ customerId: "C1", totalAmount: 10 }]}
        initialSales={[]}
      />,
    );

    expect(screen.getByTestId("by-product")).toHaveTextContent("1");
    expect(screen.queryByRole("status")).not.toBeInTheDocument();
    expect(fetchSpy).not.toHaveBeenCalled();
  });

  it("shows a warming message and triggers a refresh when starting empty", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => ({ byProduct: [], byCustomer: [] }),
    } as Response);

    render(
      <Dashboard initialByProduct={[]} initialByCustomer={[]} initialSales={[]} />,
    );

    expect(await screen.findByRole("status")).toHaveTextContent(
      /Calentando la demo/i,
    );
    await waitFor(() =>
      expect(fetchSpy).toHaveBeenCalledWith("/api/dashboard", { method: "POST" }),
    );
  });
});
