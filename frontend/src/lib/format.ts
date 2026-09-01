/** Shared display formatters. Kept in one place so every surface renders money and dates
 *  identically - these were previously copy-pasted into six different components. */

const priceFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
});

/** Same formatter instance reused across calls - Intl.NumberFormat construction is not free. */
export function formatPrice(price: number): string {
  return priceFormatter.format(price);
}

/** Whole-dollar variant used by the price filter slider, where cents are noise. */
const wholeDollarFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  maximumFractionDigits: 0,
});

export function formatWholeDollars(price: number): string {
  return wholeDollarFormatter.format(price);
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}
