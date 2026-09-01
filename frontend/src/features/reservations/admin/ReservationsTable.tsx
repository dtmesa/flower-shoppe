import { X } from "lucide-react";
import type { PickupRequest, ReservationStatus } from "../types";
import { formatDate } from "../../../lib/format";

const STATUS_OPTIONS: ReservationStatus[] = ["NEW", "CONTACTED", "CONFIRMED", "COMPLETED", "CANCELLED"];

function formatStatusLabel(status: ReservationStatus): string {
  return status.charAt(0) + status.slice(1).toLowerCase();
}

interface ReservationsTableProps {
  reservations: PickupRequest[];
  onStatusChange: (id: number, status: ReservationStatus) => void;
  onCompleteRequest: (request: PickupRequest) => void;
  onView: (request: PickupRequest) => void;
  onDelete: (request: PickupRequest) => void;
}

export function ReservationsTable({
  reservations,
  onStatusChange,
  onCompleteRequest,
  onView,
  onDelete,
}: ReservationsTableProps) {
  if (reservations.length === 0) {
    return <p className="state-message">No pickup requests yet.</p>;
  }

  return (
    <div className="table-wrapper">
      <div className="table-scroll">
      <table>
        <thead>
          <tr>
            <th>Requested</th>
            <th>Customer</th>
            <th>Email</th>
            <th>Phone</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {reservations.map((request) => (
            <tr key={request.id}>
              <td>{formatDate(request.createdAt)}</td>
              <td>{request.customerName}</td>
              <td>{request.customerEmail || "—"}</td>
              <td>{request.customerPhone || "—"}</td>
              <td>
                <select
                  value={request.status}
                  onChange={(event) => {
                    const status = event.target.value as ReservationStatus;
                    if (status === "COMPLETED") {
                      onCompleteRequest(request);
                    } else {
                      onStatusChange(request.id, status);
                    }
                  }}
                  className={`status-select status-select--${request.status.toLowerCase()}`}
                >
                  {STATUS_OPTIONS.map((status) => (
                    <option key={status} value={status}>
                      {formatStatusLabel(status)}
                    </option>
                  ))}
                </select>
              </td>
              <td>
                <div className="table-actions">
                  <button type="button" className="btn btn-secondary btn-small" onClick={() => onView(request)}>
                    View Request
                  </button>
                  <button
                    type="button"
                    className="row-delete-btn"
                    onClick={() => onDelete(request)}
                    aria-label="Delete pickup request"
                    title="Delete pickup request"
                  >
                    <X size={16} strokeWidth={2.5} aria-hidden="true" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      </div>
    </div>
  );
}
