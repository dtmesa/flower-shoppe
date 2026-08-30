import { useState } from "react";
import type { InventoryItem } from "../types";
import { deleteInventoryItem, useInventory } from "../inventoryApi";
import { extractErrorMessage } from "../../../lib/apiClient";
import { InventoryTable } from "./InventoryTable";
import { InventoryFormModal } from "./InventoryFormModal";

export function AdminInventoryPage() {
  const { items, error, isLoading, refresh } = useInventory();
  const [editingItem, setEditingItem] = useState<InventoryItem | "new" | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  async function handleDelete(item: InventoryItem) {
    if (!window.confirm(`Delete "${item.id}"? This cannot be undone.`)) return;
    setDeleteError(null);
    try {
      await deleteInventoryItem(item.id);
      await refresh();
    } catch (err) {
      setDeleteError(extractErrorMessage(err));
    }
  }

  return (
    <div>
      <div className="tab-toolbar">
        <button type="button" className="btn btn-primary" onClick={() => setEditingItem("new")}>
          Add Item
        </button>
      </div>

      {(error || deleteError) && (
        <p className="state-message state-message--error">{deleteError ?? extractErrorMessage(error)}</p>
      )}
      {isLoading && <p className="state-message">Loading...</p>}

      {!isLoading && <InventoryTable items={items} onEdit={setEditingItem} onDelete={handleDelete} />}

      {editingItem && (
        <InventoryFormModal
          item={editingItem === "new" ? undefined : editingItem}
          onClose={() => {
            setEditingItem(null);
            refresh();
          }}
          onSaved={() => refresh()}
        />
      )}
    </div>
  );
}
