import { describe, expect, it } from "vitest";
import { formatAmountCompact, formatAmountFull } from "./format";

describe("formatAmountCompact", () => {
  it("collapses thousands into K", () => {
    expect(formatAmountCompact(800_000)).toBe("800K");
  });

  it("collapses millions into M with one decimal when needed", () => {
    expect(formatAmountCompact(1_500_000)).toBe("1.5M");
  });

  it("leaves small numbers as-is", () => {
    expect(formatAmountCompact(42)).toBe("42");
  });
});

describe("formatAmountFull", () => {
  it("inserts thousands separators", () => {
    expect(formatAmountFull(649_350)).toBe("649,350");
  });

  it("keeps decimals up to two places", () => {
    expect(formatAmountFull(125.5)).toBe("125.5");
  });
});
