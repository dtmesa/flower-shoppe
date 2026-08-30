export type ReservationStatus = "PENDING" | "CONTACTED" | "COMPLETED" | "CANCELLED";

export interface Reservation {
  id: number;
  inventoryItemId: string | null;
  itemSnapshot: string;
  customerName: string;
  customerPhone: string | null;
  customerEmail: string | null;
  quantityRequested: number;
  notes: string | null;
  status: ReservationStatus;
  createdAt: string;
}

export interface ReservationInput {
  inventoryItemId: string;
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  quantityRequested: number;
  notes: string;
}
