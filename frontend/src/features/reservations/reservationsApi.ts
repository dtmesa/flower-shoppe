import useSWR from "swr";
import { apiClient, fetcher } from "../../lib/apiClient";
import type { Reservation, ReservationInput, ReservationStatus } from "./types";

const RESERVATIONS_KEY = "/api/reservations";

export function useReservations() {
  const { data, error, isLoading, mutate } = useSWR<Reservation[]>(RESERVATIONS_KEY, fetcher);
  return { reservations: data ?? [], error, isLoading, refresh: mutate };
}

export async function createReservation(input: ReservationInput): Promise<Reservation> {
  const { data } = await apiClient.post<Reservation>(RESERVATIONS_KEY, input);
  return data;
}

export async function updateReservationStatus(id: number, status: ReservationStatus): Promise<Reservation> {
  const { data } = await apiClient.patch<Reservation>(`${RESERVATIONS_KEY}/${id}/status`, { status });
  return data;
}
