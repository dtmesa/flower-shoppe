import { describe, expect, it } from "vitest";
import { getCoverImage, getCoverImageIndex } from "./imageHelpers";
import type { InventoryItem, InventoryImage } from "./types";

function image(id: number, sortOrder: number, isPrimary: boolean): InventoryImage {
  return { id, url: `/uploads/${id}.png`, sortOrder, isPrimary };
}

function item(images: InventoryImage[]): InventoryItem {
  return {
    id: "CRL",
    type: "Cutting",
    color: "Red",
    size: "Large",
    price: 18.25,
    quantityTotal: 4,
    quantityReserved: 0,
    quantityAvailable: 4,
    description: null,
    images,
    createdAt: "2026-08-30T00:00:00Z",
    updatedAt: "2026-08-30T00:00:00Z",
  };
}

describe("getCoverImage", () => {
  it("returns undefined when the item has no photos", () => {
    expect(getCoverImage(item([]))).toBeUndefined();
  });

  it("returns the only photo when there is one", () => {
    expect(getCoverImage(item([image(1, 0, true)]))?.id).toBe(1);
  });

  // The regression this helper exists to prevent: the API returns photos in upload order, so
  // images[0] is NOT the admin's chosen thumbnail once they pick a later one.
  it("prefers the primary photo over the first-uploaded one", () => {
    const images = [image(9, 0, false), image(10, 1, false), image(11, 2, true)];
    expect(getCoverImage(item(images))?.id).toBe(11);
  });

  it("falls back to the first photo when none is flagged primary", () => {
    const images = [image(9, 0, false), image(10, 1, false)];
    expect(getCoverImage(item(images))?.id).toBe(9);
  });
});

describe("getCoverImageIndex", () => {
  it("returns the primary photo's position", () => {
    const images = [image(9, 0, false), image(10, 1, false), image(11, 2, true)];
    expect(getCoverImageIndex(item(images))).toBe(2);
  });

  it("returns 0 when nothing is flagged primary", () => {
    expect(getCoverImageIndex(item([image(9, 0, false)]))).toBe(0);
  });

  it("returns 0 for an item with no photos", () => {
    expect(getCoverImageIndex(item([]))).toBe(0);
  });
});
