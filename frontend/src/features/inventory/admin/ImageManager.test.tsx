import { describe, expect, it, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { ImageManager } from "./ImageManager";
import { MAX_IMAGE_UPLOAD_BYTES, MAX_IMAGE_UPLOAD_MESSAGE } from "../inventoryApi";
import type { InventoryItem } from "../types";

vi.mock("../inventoryApi", async (importOriginal) => ({
  // The size constants are the thing under test, so they come from the real module; only the
  // network calls are stubbed.
  ...(await importOriginal<typeof import("../inventoryApi")>()),
  uploadInventoryImage: vi.fn(),
  deleteInventoryImage: vi.fn(),
  setPrimaryInventoryImage: vi.fn(),
}));

const { uploadInventoryImage } = await import("../inventoryApi");

const item: InventoryItem = {
  id: "RYM",
  type: "Rooted Plant",
  color: "Yellow/White",
  size: "Medium",
  price: 24.99,
  quantityTotal: 5,
  quantityReserved: 0,
  quantityAvailable: 5,
  description: null,
  images: [],
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
};

/** A PNG of an exact byte length, standing in for a photo off a phone camera. */
function pngOfSize(bytes: number) {
  return new File([new Uint8Array(bytes)], "photo.png", { type: "image/png" });
}

function fileInput(container: HTMLElement) {
  return container.querySelector<HTMLInputElement>('input[type="file"]')!;
}

describe("ImageManager upload size limit", () => {
  beforeEach(() => vi.clearAllMocks());

  it("refuses an oversized photo without uploading it", async () => {
    const { container } = render(<ImageManager item={item} onItemUpdated={vi.fn()} />);

    await userEvent.upload(fileInput(container), pngOfSize(MAX_IMAGE_UPLOAD_BYTES + 1));

    expect(await screen.findByText(MAX_IMAGE_UPLOAD_MESSAGE)).toBeInTheDocument();
    // The point of the check: past this size the request never reaches the API intact, so it
    // must not be sent at all.
    expect(uploadInventoryImage).not.toHaveBeenCalled();
  });

  it("uploads a photo that is exactly at the limit", async () => {
    vi.mocked(uploadInventoryImage).mockResolvedValue(item);
    const onItemUpdated = vi.fn();
    const { container } = render(<ImageManager item={item} onItemUpdated={onItemUpdated} />);

    await userEvent.upload(fileInput(container), pngOfSize(MAX_IMAGE_UPLOAD_BYTES));

    await waitFor(() => expect(uploadInventoryImage).toHaveBeenCalledTimes(1));
    expect(screen.queryByText(MAX_IMAGE_UPLOAD_MESSAGE)).not.toBeInTheDocument();
    expect(onItemUpdated).toHaveBeenCalledWith(item);
  });

  // The input reports no change event when the same file is chosen twice running, so it has to be
  // reset after a rejection - otherwise the admin's second attempt at the same file does nothing.
  it("still reacts when the same oversized photo is picked again", async () => {
    const { container } = render(<ImageManager item={item} onItemUpdated={vi.fn()} />);
    const input = fileInput(container);

    await userEvent.upload(input, pngOfSize(MAX_IMAGE_UPLOAD_BYTES + 1));
    expect(await screen.findByText(MAX_IMAGE_UPLOAD_MESSAGE)).toBeInTheDocument();
    expect(input.value).toBe("");

    await userEvent.upload(input, pngOfSize(MAX_IMAGE_UPLOAD_BYTES + 1));
    expect(await screen.findByText(MAX_IMAGE_UPLOAD_MESSAGE)).toBeInTheDocument();
    expect(uploadInventoryImage).not.toHaveBeenCalled();
  });
});
