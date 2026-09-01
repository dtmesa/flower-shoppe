import useSWR from "swr";
import { apiClient, fetcher } from "../../lib/apiClient";
import type { InventoryItem, InventoryItemCreateInput, InventoryItemUpdateInput } from "./types";

const INVENTORY_KEY = "/api/inventory";

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
