import { afterEach, describe, expect, it, vi } from "vitest";
import { fetchDashboard } from "./dashboard";

afterEach(() => {
  vi.restoreAllMocks();
});

describe("fetchDashboard", () => {
  it("returns the sales on a successful response", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: true,
      json: async () => [
        { date: "2026-01-01", customerId: "C1", productName: "A", quantity: 1, amount: 10 },
      ],
    } as Response);

    const { sales } = await fetchDashboard();

    expect(sales).toHaveLength(1);
  });

  it("throws when the backend responds with an error", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue({
      ok: false,
      status: 502,
    } as Response);

    await expect(fetchDashboard()).rejects.toThrow(/502/);
  });
});
