import { useState, type FormEvent } from "react";
import { COLOR_OPTIONS, SIZE_OPTIONS, TYPE_OPTIONS, type InventoryItem, type InventoryItemInput } from "../types";
import { Modal } from "../../../components/Modal";
import { ImageManager } from "./ImageManager";
import { createInventoryItem, updateInventoryItem } from "../inventoryApi";
import { extractErrorMessage } from "../../../lib/apiClient";

interface InventoryFormModalProps {
  item?: InventoryItem;
  onClose: () => void;
  onSaved: () => void;
}

const emptyForm: InventoryItemInput = {
  type: "",
  color: "",
  size: "",
  price: 0,
  quantityAvailable: 0,
  description: "",
};

export function InventoryFormModal({ item, onClose, onSaved }: InventoryFormModalProps) {
  const [currentItem, setCurrentItem] = useState<InventoryItem | undefined>(item);
  const [idTag, setIdTag] = useState("");
  const [form, setForm] = useState<InventoryItemInput>(
    item
      ? {
          type: item.type,
          color: item.color ?? "",
          size: item.size ?? "",
          price: item.price,
          quantityAvailable: item.quantityAvailable,
          description: item.description ?? "",
        }
      : emptyForm,
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  function updateField<K extends keyof InventoryItemInput>(key: K, value: InventoryItemInput[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSaving(true);
    try {
      const saved = currentItem
        ? await updateInventoryItem(currentItem.id, form)
        : await createInventoryItem(idTag.trim(), form);
      setCurrentItem(saved);
      onSaved();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal title={currentItem ? `Edit ${currentItem.id}` : "Add Inventory Item"} onClose={onClose} wide>
      <form className="form" onSubmit={handleSubmit}>
        {error && <p className="form-error">{error}</p>}
        <div className="form-grid">
          <label>
            ID tag
            {currentItem ? (
              <input type="text" value={currentItem.id} disabled />
            ) : (
              <input
                type="text"
                required
                placeholder="Matches the physical tag on the plant"
                value={idTag}
                onChange={(e) => setIdTag(e.target.value)}
              />
            )}
          </label>
          <label>
            Type
            <select value={form.type} onChange={(e) => updateField("type", e.target.value)} required>
              <option value="">Select a type</option>
              {TYPE_OPTIONS.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>
          <label>
            Color
            <select value={form.color} onChange={(e) => updateField("color", e.target.value)}>
              <option value="">Select a color</option>
              {COLOR_OPTIONS.map((color) => (
                <option key={color} value={color}>
                  {color}
                </option>
              ))}
            </select>
          </label>
          <label>
            Size
            <select value={form.size} onChange={(e) => updateField("size", e.target.value)}>
              <option value="">Select a size</option>
              {SIZE_OPTIONS.map((size) => (
                <option key={size} value={size}>
                  {size}
                </option>
              ))}
            </select>
          </label>
          <label>
            Price ($)
            <input
              type="number"
              required
              min={0}
              step="0.01"
              value={form.price}
              onChange={(e) => updateField("price", Number(e.target.value))}
            />
          </label>
          <label>
            Quantity available
            <input
              type="number"
              required
              min={0}
              value={form.quantityAvailable}
              onChange={(e) => updateField("quantityAvailable", Number(e.target.value))}
            />
          </label>
        </div>
        <label>
          Description
          <textarea
            rows={3}
            value={form.description}
            onChange={(e) => updateField("description", e.target.value)}
          />
        </label>
        <button type="submit" className="btn btn-primary" disabled={saving}>
          {saving ? "Saving..." : currentItem ? "Save Changes" : "Create Item"}
        </button>
      </form>

      {currentItem ? (
        <ImageManager item={currentItem} onItemUpdated={(updated) => { setCurrentItem(updated); onSaved(); }} />
      ) : (
        <p className="state-message">Save the item first, then you can add photos.</p>
      )}
    </Modal>
  );
}
