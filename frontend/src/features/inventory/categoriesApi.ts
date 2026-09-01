import useSWR from "swr";
import { apiClient, fetcher } from "../../lib/apiClient";

export type CategoryKind = "TYPE" | "COLOR" | "SIZE";

export interface Category {
  id: number;
  kind: CategoryKind;
  name: string;
  code: string;
}

const CATEGORIES_KEY = "/api/categories";

export function useCategories() {
  const { data, error, isLoading, mutate } = useSWR<Category[]>(CATEGORIES_KEY, fetcher);
  const categories = data ?? [];

  return {
    categories,
    types: categories.filter((category) => category.kind === "TYPE"),
    colors: categories.filter((category) => category.kind === "COLOR"),
    sizes: categories.filter((category) => category.kind === "SIZE"),
    error,
    isLoading,
    refresh: mutate,
  };
}

export async function createCategory(kind: CategoryKind, name: string, code: string): Promise<Category> {
  const { data } = await apiClient.post<Category>(CATEGORIES_KEY, { kind, name, code });
  return data;
}

export async function updateCategory(id: number, name: string, code: string): Promise<Category> {
  const { data } = await apiClient.put<Category>(`${CATEGORIES_KEY}/${id}`, { name, code });
  return data;
}

export async function deleteCategory(id: number): Promise<void> {
  await apiClient.delete(`${CATEGORIES_KEY}/${id}`);
}
