import { useState } from "react";
import type { ReservationStatus } from "../types";
import { updateReservationStatus, useReservations } from "../reservationsApi";
import { extractErrorMessage } from "../../../lib/apiClient";
import { ReservationsTable } from "./ReservationsTable";

export function AdminReservationsPage() {
  const { reservations, error, isLoading, refresh } = useReservations();
  const [updateError, setUpdateError] = useState<string | null>(null);

  async function handleStatusChange(id: number, status: ReservationStatus) {
    setUpdateError(null);
    try {
      await updateReservationStatus(id, status);
      await refresh();
    } catch (err) {
      setUpdateError(extractErrorMessage(err));
    }
  }

  return (
    <div>
      {(error || updateError) && (
        <p className="state-message state-message--error">{updateError ?? extractErrorMessage(error)}</p>
      )}
      {isLoading && <p className="state-message">Loading...</p>}

      {!isLoading && <ReservationsTable reservations={reservations} onStatusChange={handleStatusChange} />}
    </div>
  );
}
