import { CircleX, Info } from "lucide-react";
import type { PickupRequest, ReservationStatus } from "../types";
import { formatDate } from "../../../lib/format";
import { StatusDropdown } from "./StatusDropdown";

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
    return <p className="state-message state-message--intro">No pickup requests yet.</p>;
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
              <td className="status-cell">
                <StatusDropdown
                  value={request.status}
                  onChange={(status: ReservationStatus) => {
                    if (status === "COMPLETED") {
                      onCompleteRequest(request);
                    } else {
                      onStatusChange(request.id, status);
                    }
                  }}
                />
              </td>
              <td className="table-actions-cell">
                <div className="table-actions">
                  <button
                    type="button"
                    className="row-icon-btn"
                    onClick={() => onView(request)}
                    aria-label="View request"
                    title="View request"
                  >
                    <Info size={22} strokeWidth={2} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="row-icon-btn"
                    onClick={() => onDelete(request)}
                    aria-label="Delete pickup request"
                    title="Delete pickup request"
                  >
                    <CircleX size={22} strokeWidth={2} aria-hidden="true" />
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
