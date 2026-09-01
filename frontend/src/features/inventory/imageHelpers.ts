import type { InventoryImage, InventoryItem } from "./types";

/**
 * The photo that represents an item wherever a single thumbnail is shown (catalog card, cart
 * line, admin table, detail-modal initial view).
 *
 * The API returns images ordered by SortOrder, which is upload order - the admin's chosen
 * thumbnail can be any of them, so `images[0]` is NOT interchangeable with this. Reading the
 * flag in one shared place keeps every surface agreeing on which photo is "the" photo.
 */
export function getCoverImage(item: InventoryItem): InventoryImage | undefined {
  return item.images.find((image) => image.isPrimary) ?? item.images[0];
}

/** Index of the cover image, for galleries that track a selected position rather than an image. */
export function getCoverImageIndex(item: InventoryItem): number {
  return Math.max(
    item.images.findIndex((image) => image.isPrimary),
    0,
  );
}
