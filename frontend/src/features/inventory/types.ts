export interface InventoryImage {
  id: number;
  url: string;
  sortOrder: number;
  isPrimary: boolean;
}

export interface InventoryItem {
  id: string;
  type: string;
  color: string | null;
  size: string | null;
  price: number;
  /** Units physically on hand, including any held by confirmed pickup requests. */
  quantityTotal: number;
  /** Units held by confirmed-but-not-yet-completed pickup requests. */
  quantityReserved: number;
  /** total - reserved. What a customer can actually request; server-computed. */
  quantityAvailable: number;
  description: string | null;
  images: InventoryImage[];
  createdAt: string;
  updatedAt: string;
}

export interface InventoryItemCreateInput {
  type: string;
  color: string;
  size: string;
  price: number;
  quantityTotal: number;
  description: string;
}

export interface InventoryItemUpdateInput {
  price: number;
  quantityTotal: number;
  description: string;
}
