import { useState } from "react";
import { Plus } from "lucide-react";
import type { InventoryItem } from "../types";
import { deleteInventoryItem, useInventory } from "../inventoryApi";
import { extractErrorMessage } from "../../../lib/apiClient";
import { useConfirm } from "../../../components/ConfirmDialog";
import { InventoryTable } from "./InventoryTable";
import { InventoryFormModal } from "./InventoryFormModal";

export function AdminInventoryPage() {
  const { items, error, isLoading, refresh } = useInventory();
  const [editingItem, setEditingItem] = useState<InventoryItem | "new" | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [confirm, confirmDialog] = useConfirm();

  async function handleDelete(item: InventoryItem) {
    const proceed = await confirm({
      title: "Delete Item",
      message: `Delete "${item.id}"? This cannot be undone.`,
      confirmLabel: "Delete",
      danger: true,
      centered: true,
    });
    if (!proceed) return;
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
      {(error || deleteError) && (
        <p className="state-message state-message--error">{deleteError ?? extractErrorMessage(error)}</p>
      )}
      <div className="inventory-table-area">
        <button
          type="button"
          className="btn-icon-circle btn-icon-circle--corner"
          onClick={() => setEditingItem("new")}
          title="New Item"
          aria-label="New Item"
        >
          <Plus size={20} strokeWidth={2.5} />
        </button>
        {!isLoading && <InventoryTable items={items} onEdit={setEditingItem} onDelete={handleDelete} />}
      </div>

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

      {confirmDialog}
    </div>
  );
}
