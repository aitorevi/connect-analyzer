import { render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import RevenueOverTimeChart from "./RevenueOverTimeChart";

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

describe("RevenueOverTimeChart", () => {
  it("renders without crashing for valid data", () => {
    const { getByTestId } = render(
      <RevenueOverTimeChart data={[{ date: "2026-01-01", total: 100 }]} />,
    );

    expect(getByTestId("rc-mock")).toBeInTheDocument();
  });

  it("renders an empty state instead of the chart when there is no data", () => {
    const { getByTestId, queryByTestId } = render(
      <RevenueOverTimeChart data={[]} />,
    );

    expect(getByTestId("empty-over-time")).toBeInTheDocument();
    expect(queryByTestId("rc-mock")).toBeNull();
  });
});
