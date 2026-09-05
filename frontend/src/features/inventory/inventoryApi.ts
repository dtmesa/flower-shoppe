import useSWR from "swr";
import { apiClient, fetcher } from "../../lib/apiClient";
import type { InventoryItem, InventoryItemCreateInput, InventoryItemUpdateInput } from "./types";

const INVENTORY_KEY = "/api/inventory";

/**
 * Largest photo the API accepts, mirroring `App:Storage:MaxSizeBytes` in the backend's
 * appsettings.json.
 *
 * The server is still the one enforcing this; checking here only changes where the admin finds
 * out. It has to, because the API runs as a Lambda function: a multipart upload reaches it
 * base64-encoded, and much past this size the request trips Lambda's payload limit and is
 * rejected by API Gateway before the API can answer with its own message - leaving the admin
 * with a bare "Request failed with status code 413". Refusing the file here means one clear
 * message either way, and no pointless multi-megabyte upload.
 */
export const MAX_IMAGE_UPLOAD_BYTES = 4 * 1024 * 1024;

/** Worded to match the API's own rejection, so the limit reads the same wherever it's hit. */
export const MAX_IMAGE_UPLOAD_MESSAGE = `Image exceeds maximum size of ${MAX_IMAGE_UPLOAD_BYTES / 1024 / 1024}MB`;

/**
 * @param enabled pass false to skip the request entirely (SWR treats a null key as "don't
 *   fetch"). Used by the cart, which is mounted app-wide but has nothing to show on admin
 *   routes and shouldn't pull the whole catalog there.
 */
export function useInventory(enabled = true) {
  const { data, error, isLoading, mutate } = useSWR<InventoryItem[]>(
    enabled ? INVENTORY_KEY : null,
    fetcher,
  );
  return { items: data ?? [], error, isLoading, refresh: mutate };
}

export async function createInventoryItem(input: InventoryItemCreateInput): Promise<InventoryItem> {
  const { data } = await apiClient.post<InventoryItem>(INVENTORY_KEY, input);
  return data;
}

export async function updateInventoryItem(id: string, input: InventoryItemUpdateInput): Promise<InventoryItem> {
  const { data } = await apiClient.put<InventoryItem>(`${INVENTORY_KEY}/${id}`, input);
  return data;
}

export async function deleteInventoryItem(id: string): Promise<void> {
  await apiClient.delete(`${INVENTORY_KEY}/${id}`);
}

export async function uploadInventoryImage(id: string, file: File): Promise<InventoryItem> {
  const formData = new FormData();
  formData.append("file", file);
  const { data } = await apiClient.post<InventoryItem>(`${INVENTORY_KEY}/${id}/images`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data;
}

export async function deleteInventoryImage(itemId: string, imageId: number): Promise<InventoryItem> {
  const { data } = await apiClient.delete<InventoryItem>(`${INVENTORY_KEY}/${itemId}/images/${imageId}`);
  return data;
}

export async function setPrimaryInventoryImage(itemId: string, imageId: number): Promise<InventoryItem> {
  const { data } = await apiClient.post<InventoryItem>(`${INVENTORY_KEY}/${itemId}/images/${imageId}/primary`);
  return data;
}
