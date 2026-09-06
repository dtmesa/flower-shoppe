import useSWR from "swr";
import { apiClient, fetcher } from "../../lib/apiClient";
import type { PickupRequest, PickupRequestInput, ReservationStatus } from "./types";

const RESERVATIONS_KEY = "/api/reservations";

/** Stable empty array for the not-yet-loaded case - see NO_ITEMS in inventoryApi.ts. */
const NO_RESERVATIONS: PickupRequest[] = [];

export function useReservations() {
  const { data, error, isLoading, mutate } = useSWR<PickupRequest[]>(RESERVATIONS_KEY, fetcher);
  return { reservations: data ?? NO_RESERVATIONS, error, isLoading, refresh: mutate };
}

export async function createPickupRequest(input: PickupRequestInput): Promise<PickupRequest> {
  const { data } = await apiClient.post<PickupRequest>(RESERVATIONS_KEY, input);
  return data;
}

export async function updateReservationStatus(id: number, status: ReservationStatus): Promise<PickupRequest> {
  const { data } = await apiClient.patch<PickupRequest>(`${RESERVATIONS_KEY}/${id}/status`, { status });
  return data;
}

export async function completeReservation(id: number, permanentlyClear: boolean): Promise<PickupRequest> {
  const { data } = await apiClient.post<PickupRequest>(`${RESERVATIONS_KEY}/${id}/complete`, { permanentlyClear });
  return data;
}

export async function deleteReservation(id: number): Promise<void> {
  await apiClient.delete(`${RESERVATIONS_KEY}/${id}`);
}
