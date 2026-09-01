export type ReservationStatus = "NEW" | "CONTACTED" | "CONFIRMED" | "COMPLETED" | "CANCELLED";

export interface ReservationLine {
  id: number;
  inventoryItemId: string | null;
  itemSnapshot: string;
  quantityRequested: number;
}

export interface PickupRequest {
  id: number;
  customerName: string;
  customerPhone: string | null;
  customerEmail: string | null;
  notes: string | null;
  status: ReservationStatus;
  stockReserved: boolean;
  createdAt: string;
  items: ReservationLine[];
}

export interface PickupRequestLineItemInput {
  inventoryItemId: string;
  quantityRequested: number;
}

export interface PickupRequestInput {
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  notes: string;
  items: PickupRequestLineItemInput[];
}
