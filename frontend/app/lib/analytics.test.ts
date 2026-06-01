import { describe, expect, it } from "vitest";
import {
  computeKpis,
  customerTotals,
  dateRange,
  filterSales,
  productRevenueUnits,
  productTotals,
  revenueByDate,
  salesCountByDate,
  uniqueProducts,
} from "./analytics";
import type { Sale } from "./dashboard";

const S = (date: string, productName: string, customerId: string, amount: number): Sale => ({
  date,
  productName,
  customerId,
  quantity: 1,
  amount,
});

const SAMPLE: Sale[] = [
  S("2026-01-01", "A", "C1", 30),
  S("2026-01-02", "B", "C2", 20),
  S("2026-01-03", "A", "C2", 50),
];

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
    expect(kpis.distinctCustomers).toBe(0);
    expect(kpis.bestDayDate).toBeNull();
  });

  it("counts distinct customers/products and finds the best day", () => {
    const sales: Sale[] = [
      { date: "2026-01-01", customerId: "C1", productName: "A", quantity: 1, amount: 30 },
      { date: "2026-01-01", customerId: "C2", productName: "B", quantity: 1, amount: 20 },
      { date: "2026-01-02", customerId: "C1", productName: "A", quantity: 1, amount: 90 },
    ];

    const kpis = computeKpis(sales, [], []);

    expect(kpis.distinctCustomers).toBe(2);
    expect(kpis.distinctProducts).toBe(2);
    expect(kpis.bestDayDate).toBe("2026-01-02");
    expect(kpis.bestDayTotal).toBe(90);
  });
});

describe("salesCountByDate", () => {
  it("counts sales per day, sorted ascending", () => {
    const result = salesCountByDate([
      { date: "2026-01-02", customerId: "C1", productName: "A", quantity: 1, amount: 10 },
      { date: "2026-01-01", customerId: "C1", productName: "A", quantity: 1, amount: 10 },
      { date: "2026-01-02", customerId: "C2", productName: "B", quantity: 1, amount: 10 },
    ]);

    expect(result).toEqual([
      { date: "2026-01-01", count: 1 },
      { date: "2026-01-02", count: 2 },
    ]);
  });
});

describe("productRevenueUnits", () => {
  it("joins backend revenue with units summed from raw sales, keeping order", () => {
    const sales: Sale[] = [
      { date: "2026-01-01", customerId: "C1", productName: "A", quantity: 3, amount: 60 },
      { date: "2026-01-02", customerId: "C2", productName: "A", quantity: 2, amount: 40 },
      { date: "2026-01-02", customerId: "C2", productName: "B", quantity: 5, amount: 50 },
    ];

    const result = productRevenueUnits(
      [
        { product: "A", totalAmount: 100 },
        { product: "B", totalAmount: 50 },
      ],
      sales,
    );

    expect(result).toEqual([
      { product: "A", revenue: 100, units: 5 },
      { product: "B", revenue: 50, units: 5 },
    ]);
  });
});

describe("filterSales", () => {
  it("filters by inclusive date range", () => {
    const result = filterSales(SAMPLE, {
      from: "2026-01-02",
      to: "2026-01-02",
      products: [],
      customers: [],
    });
    expect(result).toHaveLength(1);
    expect(result[0].productName).toBe("B");
  });

  it("filters by selected products and customers", () => {
    const result = filterSales(SAMPLE, {
      from: null,
      to: null,
      products: ["A"],
      customers: ["C2"],
    });
    expect(result).toEqual([S("2026-01-03", "A", "C2", 50)]);
  });

  it("returns all sales when no filter is set", () => {
    expect(
      filterSales(SAMPLE, { from: null, to: null, products: [], customers: [] }),
    ).toHaveLength(3);
  });
});

describe("productTotals / customerTotals", () => {
  it("groups and sorts by amount desc", () => {
    expect(productTotals(SAMPLE)).toEqual([
      { product: "A", totalAmount: 80 },
      { product: "B", totalAmount: 20 },
    ]);
    expect(customerTotals(SAMPLE)).toEqual([
      { customerId: "C2", totalAmount: 70 },
      { customerId: "C1", totalAmount: 30 },
    ]);
  });
});

describe("dateRange / uniqueProducts", () => {
  it("returns the min and max dates and the sorted distinct products", () => {
    expect(dateRange(SAMPLE)).toEqual({ min: "2026-01-01", max: "2026-01-03" });
    expect(uniqueProducts(SAMPLE)).toEqual(["A", "B"]);
  });

  it("handles an empty dataset", () => {
    expect(dateRange([])).toEqual({ min: null, max: null });
  });
});
