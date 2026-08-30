export const TYPE_OPTIONS = ["Cutting", "Rooted Plant"] as const;
export const COLOR_OPTIONS = ["Red", "Pink", "Yellow/White"] as const;
export const SIZE_OPTIONS = ["Small", "Medium", "Large"] as const;

export interface InventoryImage {
  id: number;
  url: string;
  sortOrder: number;
}

export interface InventoryItem {
  id: string;
  type: string;
  color: string | null;
  size: string | null;
  price: number;
  quantityAvailable: number;
  description: string | null;
  images: InventoryImage[];
  createdAt: string;
  updatedAt: string;
}

export interface InventoryItemInput {
  type: string;
  color: string;
  size: string;
  price: number;
  quantityAvailable: number;
  description: string;
}
