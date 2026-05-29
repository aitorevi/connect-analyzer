import { render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ByCustomerChart from "./ByCustomerChart";

// Recharts' ResponsiveContainer needs real layout dimensions, which jsdom does not
// compute. Replace it with a fixed-size passthrough so the inner chart tree mounts
// without crashing. We don't assert on the SVG output because recharts itself does
// not render meaningful content in jsdom — instead we verify the component tree
// mounts and our mock is wired in.
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

describe("ByCustomerChart", () => {
  it("renders without crashing for valid data", () => {
    const { getByTestId } = render(
      <ByCustomerChart data={[{ customerId: "C001", totalAmount: 42 }]} />
    );

    expect(getByTestId("rc-mock")).toBeInTheDocument();
  });

  it("renders an empty state instead of the chart when there is no data", () => {
    const { getByTestId, queryByTestId } = render(<ByCustomerChart data={[]} />);

    expect(getByTestId("empty-by-customer")).toBeInTheDocument();
    expect(queryByTestId("rc-mock")).toBeNull();
  });
});
