import type { Reservation, ReservationStatus } from "../types";

const STATUS_OPTIONS: ReservationStatus[] = ["PENDING", "CONTACTED", "COMPLETED", "CANCELLED"];

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

interface ReservationsTableProps {
  reservations: Reservation[];
  onStatusChange: (id: number, status: ReservationStatus) => void;
}

export function ReservationsTable({ reservations, onStatusChange }: ReservationsTableProps) {
  if (reservations.length === 0) {
    return <p className="state-message">No pickup requests yet.</p>;
  }

  return (
    <div className="table-wrapper">
      <table>
        <thead>
          <tr>
            <th>Requested</th>
            <th>Item</th>
            <th>Qty</th>
            <th>Customer</th>
            <th>Contact</th>
            <th>Notes</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {reservations.map((reservation) => (
            <tr key={reservation.id}>
              <td>{formatDate(reservation.createdAt)}</td>
              <td>{reservation.itemSnapshot}</td>
              <td>{reservation.quantityRequested}</td>
              <td>{reservation.customerName}</td>
              <td>
                {[reservation.customerPhone, reservation.customerEmail].filter(Boolean).join(" / ") || "—"}
              </td>
              <td className="table-notes">{reservation.notes || "—"}</td>
              <td>
                <select
                  value={reservation.status}
                  onChange={(event) => onStatusChange(reservation.id, event.target.value as ReservationStatus)}
                  className={`status-select status-select--${reservation.status.toLowerCase()}`}
                >
                  {STATUS_OPTIONS.map((status) => (
                    <option key={status} value={status}>
                      {status}
                    </option>
                  ))}
                </select>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
