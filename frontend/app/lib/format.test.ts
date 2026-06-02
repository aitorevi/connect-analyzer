import { describe, expect, it } from "vitest";
import {
  formatAmountCompact,
  formatAmountFull,
  formatDateShort,
  formatInt,
} from "./format";

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

describe("formatInt", () => {
  it("formats integers with thousands separators and no decimals", () => {
    expect(formatInt(1_234_567)).toBe("1,234,567");
    expect(formatInt(42)).toBe("42");
  });
});

describe("formatDateShort", () => {
  it("formats an ISO date as a short month/day label", () => {
    expect(formatDateShort("2026-01-06")).toBe("Jan 6");
  });

  it("returns the raw input when it is not a valid Y-M-D date", () => {
    expect(formatDateShort("not-a-date")).toBe("not-a-date");
  });
});
