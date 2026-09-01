import { describe, expect, it } from "vitest";
import { formatPrice, formatWholeDollars, formatDate } from "./format";

describe("formatPrice", () => {
  it("renders two decimal places", () => {
    expect(formatPrice(6)).toBe("$6.00");
    expect(formatPrice(18.25)).toBe("$18.25");
  });

  it("groups thousands", () => {
    expect(formatPrice(1234.5)).toBe("$1,234.50");
  });

  it("rounds to cents", () => {
    expect(formatPrice(9.999)).toBe("$10.00");
  });
});

describe("formatWholeDollars", () => {
  // Used by the max-price filter, where cents would be noise.
  it("drops the cents", () => {
    expect(formatWholeDollars(45)).toBe("$45");
    expect(formatWholeDollars(45.75)).toBe("$46");
  });
});

describe("formatDate", () => {
  it("renders a medium date with a short time", () => {
    const formatted = formatDate("2026-08-30T20:04:00.000Z");
    // Exact text is locale/timezone dependent, so assert on shape rather than a fixed string.
    expect(formatted).toMatch(/\d{4}/);
    expect(formatted).toMatch(/\d{1,2}:\d{2}/);
  });
});
