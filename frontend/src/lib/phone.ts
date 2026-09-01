/** Digits the backend requires for a US phone number (see ReservationService.CreateAsync). */
const REQUIRED_DIGITS = 10;

export function phoneDigits(value: string): string {
  return value.replace(/\D/g, "");
}

/**
 * Formats progressively as the customer types: digits only, capped at 10, shaped into
 * "(xxx) xxx-xxxx" as each group fills in rather than all at once at the end.
 */
export function formatPhoneInput(value: string): string {
  const digits = phoneDigits(value).slice(0, REQUIRED_DIGITS);
  const area = digits.slice(0, 3);
  const prefix = digits.slice(3, 6);
  const line = digits.slice(6, 10);

  if (digits.length <= 3) return area;
  if (digits.length <= 6) return `(${area}) ${prefix}`;
  return `(${area}) ${prefix}-${line}`;
}

/** Mirrors the backend's rule: exactly 10 digits, ignoring any formatting characters. */
export function isCompletePhone(value: string): boolean {
  return phoneDigits(value).length === REQUIRED_DIGITS;
}
