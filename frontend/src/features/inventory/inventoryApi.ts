import useSWR from "swr";
import { apiClient, fetcher } from "../../lib/apiClient";
import type { InventoryItem, InventoryItemInput } from "./types";

const INVENTORY_KEY = "/api/inventory";

export function useInventory() {
  const { data, error, isLoading, mutate } = useSWR<InventoryItem[]>(INVENTORY_KEY, fetcher);
  return { items: data ?? [], error, isLoading, refresh: mutate };
}

export async function createInventoryItem(id: string, input: InventoryItemInput): Promise<InventoryItem> {
  const { data } = await apiClient.post<InventoryItem>(INVENTORY_KEY, { id, ...input });
  return data;
}

export async function updateInventoryItem(id: string, input: InventoryItemInput): Promise<InventoryItem> {
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
