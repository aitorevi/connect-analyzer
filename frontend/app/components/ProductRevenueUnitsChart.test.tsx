import { render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ProductRevenueUnitsChart from "./ProductRevenueUnitsChart";

// Recharts' ResponsiveContainer needs real layout dimensions, which jsdom does not
// compute. Replace it with a fixed-size passthrough so the inner chart tree mounts.
vi.mock("recharts", async () => {
  const actual = await vi.importActual<typeof import("recharts")>("recharts");
  return {
    ...actual,
    ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
      <div data-testid="rc-mock" style={{ width: 500, height: 300 }}>
        {children}
      </div>
    ),
  };
});

describe("ProductRevenueUnitsChart", () => {
  it("renders without crashing for valid data", () => {
    const { getByTestId } = render(
      <ProductRevenueUnitsChart data={[{ product: "P", revenue: 100, units: 5 }]} />,
    );

    expect(getByTestId("rc-mock")).toBeInTheDocument();
  });

  it("renders an empty state instead of the chart when there is no data", () => {
    const { getByTestId, queryByTestId } = render(
      <ProductRevenueUnitsChart data={[]} />,
    );

    expect(getByTestId("empty-product")).toBeInTheDocument();
    expect(queryByTestId("rc-mock")).toBeNull();
  });
});
