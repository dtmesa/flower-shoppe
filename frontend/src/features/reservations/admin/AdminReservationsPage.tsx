import { useState } from "react";
import type { PickupRequest, ReservationStatus } from "../types";
import { completeReservation, deleteReservation, updateReservationStatus, useReservations } from "../reservationsApi";
import { extractErrorMessage } from "../../../lib/apiClient";
import { ReservationsTable } from "./ReservationsTable";
import { Modal } from "../../../components/Modal";
import { useConfirm } from "../../../components/ConfirmDialog";
import { formatDate } from "../../../lib/format";

export function AdminReservationsPage() {
  const { reservations, error, isLoading, refresh } = useReservations();
  const [updateError, setUpdateError] = useState<string | null>(null);
  const [completing, setCompleting] = useState<PickupRequest | null>(null);
  const [viewing, setViewing] = useState<PickupRequest | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [confirm, confirmDialog] = useConfirm();

  async function handleStatusChange(id: number, status: ReservationStatus) {
    setUpdateError(null);
    try {
      await updateReservationStatus(id, status);
      await refresh();
    } catch (err) {
      setUpdateError(extractErrorMessage(err));
    }
  }

  function handleCompleteRequest(request: PickupRequest) {
    if (request.stockReserved) {
      setCompleting(request);
      return;
    }
    void handleComplete(request.id, true);
  }

  async function handleDelete(request: PickupRequest) {
    const proceed = await confirm({
      title: "Delete Pickup Request",
      message: `Delete the request from "${request.customerName}"? This cannot be undone.`,
      confirmLabel: "Delete",
      danger: true,
      centered: true,
    });
    if (!proceed) return;

    setUpdateError(null);
    try {
      await deleteReservation(request.id);
      await refresh();
    } catch (err) {
      setUpdateError(extractErrorMessage(err));
    }
  }

  async function handleComplete(id: number, permanentlyClear: boolean) {
    setUpdateError(null);
    setSubmitting(true);
    try {
      await completeReservation(id, permanentlyClear);
      await refresh();
      setCompleting(null);
    } catch (err) {
      setUpdateError(extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div>
      {(error || updateError) && (
        <p className="state-message state-message--error">{updateError ?? extractErrorMessage(error)}</p>
      )}
      {!isLoading && (
        <ReservationsTable
          reservations={reservations}
          onStatusChange={handleStatusChange}
          onCompleteRequest={handleCompleteRequest}
          onView={setViewing}
          onDelete={handleDelete}
        />
      )}
      {confirmDialog}

      {viewing && (
        <Modal title={`Request from ${viewing.customerName}`} onClose={() => setViewing(null)}>
          <p className="detail-description">
            <strong>Date Requested:</strong> {formatDate(viewing.createdAt)}
            <br />
            <strong>Email:</strong> {viewing.customerEmail || "—"}
            <br />
            <strong>Phone:</strong> {viewing.customerPhone || "—"}
          </p>
          <div className="table-wrapper">
            <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Item</th>
                  <th>Qty</th>
                </tr>
              </thead>
              <tbody>
                {viewing.items.map((line) => (
                  <tr key={line.id}>
                    <td>{line.itemSnapshot}</td>
                    <td>{line.quantityRequested}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            </div>
          </div>
          <p className="detail-description">{viewing.notes || "No notes attached to this request."}</p>
        </Modal>
      )}

      {completing && (
        <Modal title="Complete Pickup Request" onClose={() => setCompleting(null)}>
          <p>
            The following items have been reserved since this request was confirmed. Did the customer take them?
          </p>
          <ul>
            {completing.items.map((line) => (
              <li key={line.id}>
                {line.quantityRequested} × {line.itemSnapshot}
              </li>
            ))}
          </ul>
          <p className="state-message state-message--left">
            &quot;Clear Stock&quot; removes them from inventory for good. &quot;Restore Stock&quot; releases the
            reservation and puts them back on sale.
          </p>
          <div className="cart-checkout-actions">
            <button
              type="button"
              className="btn btn-secondary restore-stock-btn"
              disabled={submitting}
              onClick={() => handleComplete(completing.id, false)}
            >
              Restore Stock
            </button>
            <button
              type="button"
              className="btn btn-primary"
              disabled={submitting}
              onClick={() => handleComplete(completing.id, true)}
            >
              Clear Stock
            </button>
          </div>
        </Modal>
      )}
    </div>
  );
}
