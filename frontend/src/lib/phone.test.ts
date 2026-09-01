import { describe, expect, it } from "vitest";
import { formatPhoneInput, isCompletePhone, phoneDigits } from "./phone";

describe("formatPhoneInput", () => {
  it("leaves the first three digits bare", () => {
    expect(formatPhoneInput("5")).toBe("5");
    expect(formatPhoneInput("555")).toBe("555");
  });

  it("wraps the area code once a fourth digit arrives", () => {
    expect(formatPhoneInput("5551")).toBe("(555) 1");
    expect(formatPhoneInput("555123")).toBe("(555) 123");
  });

  it("adds the dash once the line number starts", () => {
    expect(formatPhoneInput("5551234")).toBe("(555) 123-4");
    expect(formatPhoneInput("5551234567")).toBe("(555) 123-4567");
  });

  it("caps at ten digits", () => {
    expect(formatPhoneInput("55512345679999")).toBe("(555) 123-4567");
  });

  it("strips non-digits, so re-formatting its own output is stable", () => {
    const once = formatPhoneInput("5551234567");
    expect(formatPhoneInput(once)).toBe(once);
  });

  it("ignores letters and punctuation the user pastes in", () => {
    expect(formatPhoneInput("(555) 123-4567 ext")).toBe("(555) 123-4567");
    expect(formatPhoneInput("555.123.4567")).toBe("(555) 123-4567");
  });

  it("returns empty for input with no digits", () => {
    expect(formatPhoneInput("abc")).toBe("");
    expect(formatPhoneInput("")).toBe("");
  });

  // Backspacing through the formatting characters must not get stuck: deleting the last
  // character of "(555) 1" leaves "(555) " -> 3 digits -> re-formats down to "555".
  it("collapses back down as the user deletes", () => {
    expect(formatPhoneInput("(555) ")).toBe("555");
  });
});

describe("isCompletePhone", () => {
  it("accepts exactly ten digits in any formatting", () => {
    expect(isCompletePhone("(555) 123-4567")).toBe(true);
    expect(isCompletePhone("5551234567")).toBe(true);
  });

  it("rejects anything short of ten digits", () => {
    expect(isCompletePhone("(555) 123-456")).toBe(false);
    expect(isCompletePhone("")).toBe(false);
  });
});

describe("phoneDigits", () => {
  it("keeps only digits", () => {
    expect(phoneDigits("(555) 123-4567")).toBe("5551234567");
  });
});
