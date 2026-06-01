import { describe, expect, it } from "vitest";
import { computeKpis, revenueByDate } from "./analytics";
import type { Sale } from "./dashboard";

const sale = (date: string, amount: number, quantity = 1): Sale => ({
  date,
  amount,
  quantity,
  customerId: "C1",
  productName: "P",
});

describe("revenueByDate", () => {
  it("groups amounts by date and sorts ascending", () => {
    const result = revenueByDate([
      sale("2026-01-02", 100),
      sale("2026-01-01", 50),
      sale("2026-01-02", 25),
    ]);

    expect(result).toEqual([
      { date: "2026-01-01", total: 50 },
      { date: "2026-01-02", total: 125 },
    ]);
  });

  it("returns an empty series when there are no sales", () => {
    expect(revenueByDate([])).toEqual([]);
  });
});

describe("computeKpis", () => {
  it("sums revenue and units, counts transactions and computes the average ticket", () => {
    const sales = [sale("2026-01-01", 100, 2), sale("2026-01-02", 50, 3)];

    const kpis = computeKpis(
      sales,
      [{ product: "P", totalAmount: 150 }],
      [{ customerId: "C1", totalAmount: 150 }],
    );

    expect(kpis.totalRevenue).toBe(150);
    expect(kpis.totalUnits).toBe(5);
    expect(kpis.transactions).toBe(2);
    expect(kpis.avgTicket).toBe(75);
    expect(kpis.topProduct).toBe("P");
    expect(kpis.topCustomer).toBe("C1");
  });

  it("handles empty input without dividing by zero", () => {
    const kpis = computeKpis([], [], []);

    expect(kpis.totalRevenue).toBe(0);
    expect(kpis.avgTicket).toBe(0);
    expect(kpis.topProduct).toBeNull();
    expect(kpis.topCustomer).toBeNull();
  });
});
