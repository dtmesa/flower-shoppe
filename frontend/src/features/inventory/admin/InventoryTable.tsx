import type { InventoryItem } from "../types";

function formatPrice(price: number): string {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(price);
}

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
      <table>
        <thead>
          <tr>
            <th>Photo</th>
            <th>ID Tag</th>
            <th>Type</th>
            <th>Color</th>
            <th>Size</th>
            <th>Price</th>
            <th>Qty</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>
                {item.images[0] ? (
                  <img
                    className="table-thumb"
                    src={`${import.meta.env.VITE_API_BASE_URL}${item.images[0].url}`}
                    alt=""
                  />
                ) : (
                  <span className="table-thumb table-thumb--empty" aria-hidden="true">
                    🌸
                  </span>
                )}
              </td>
              <td>{item.id}</td>
              <td>{item.type}</td>
              <td>{item.color || "—"}</td>
              <td>{item.size || "—"}</td>
              <td>{formatPrice(item.price)}</td>
              <td>{item.quantityAvailable}</td>
              <td className="table-actions">
                <button type="button" className="btn btn-secondary btn-small" onClick={() => onEdit(item)}>
                  Edit
                </button>
                <button type="button" className="btn btn-danger btn-small" onClick={() => onDelete(item)}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
