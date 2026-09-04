import { CircleX, Flower, SquarePen } from "lucide-react";
import type { InventoryItem } from "../types";
import { getCoverImage } from "../imageHelpers";
import { uploadUrl } from "../../../lib/apiClient";
import { formatPrice } from "../../../lib/format";

interface InventoryTableProps {
  items: InventoryItem[];
  onEdit: (item: InventoryItem) => void;
  onDelete: (item: InventoryItem) => void;
}

export function InventoryTable({ items, onEdit, onDelete }: InventoryTableProps) {
  if (items.length === 0) {
    return <p className="state-message">No inventory yet. Add your first item to get started.</p>;
  }

  return (
    <div className="table-wrapper">
      <div className="table-scroll">
      <table>
        <thead>
          <tr>
            <th>Photo</th>
            <th>ID Tag</th>
            <th>Type</th>
            <th>Color</th>
            <th>Size</th>
            <th>Price</th>
            <th>Total</th>
            <th>Reserved</th>
            <th>Available</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => {
            // Must match what the storefront shows for the same item - the admin's chosen
            // thumbnail, not simply the first-uploaded photo.
            const coverImage = getCoverImage(item);
            return (
            <tr key={item.id}>
              <td>
                {coverImage ? (
                  <img className="table-thumb" src={uploadUrl(coverImage.url)} alt="" />
                ) : (
                  <span className="table-thumb table-thumb--empty" aria-hidden="true">
                    <Flower size={20} strokeWidth={1.5} />
                  </span>
                )}
              </td>
              <td>{item.id}</td>
              <td>{item.type}</td>
              <td>{item.color || "—"}</td>
              <td>{item.size || "—"}</td>
              <td>{formatPrice(item.price)}</td>
              <td>{item.quantityTotal}</td>
              {/* Held by confirmed pickup requests. Dimmed at zero so rows with an actual
                  hold stand out at a glance. */}
              <td className={item.quantityReserved > 0 ? "qty-reserved" : "qty-zero"}>
                {item.quantityReserved}
              </td>
              <td className={item.quantityAvailable === 0 ? "qty-zero" : undefined}>
                {item.quantityAvailable}
              </td>
              <td>
                <div className="table-actions">
                  <button
                    type="button"
                    className="row-icon-btn"
                    onClick={() => onEdit(item)}
                    aria-label="Edit item"
                    title="Edit item"
                  >
                    <SquarePen size={22} strokeWidth={2} aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="row-icon-btn"
                    onClick={() => onDelete(item)}
                    aria-label="Delete item"
                    title="Delete item"
                  >
                    <CircleX size={22} strokeWidth={2} aria-hidden="true" />
                  </button>
                </div>
              </td>
            </tr>
            );
          })}
        </tbody>
      </table>
      </div>
    </div>
  );
}
