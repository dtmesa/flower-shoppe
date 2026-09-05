import { useEffect, useState, type FormEvent } from "react";
import type { InventoryItem, InventoryItemCreateInput, InventoryItemUpdateInput } from "../types";
import { Modal } from "../../../components/Modal";
import { FormError, useDismissingError } from "../../../components/FormError";
import { QuantityStepper } from "../../../components/QuantityStepper";
import { ImageManager } from "./ImageManager";
import { createInventoryItem, updateInventoryItem } from "../inventoryApi";
import { useCategories } from "../categoriesApi";
import { extractErrorMessage } from "../../../lib/apiClient";

interface InventoryFormModalProps {
  item?: InventoryItem;
  onClose: () => void;
  onSaved: () => void;
}

const emptyForm: InventoryItemCreateInput = {
  type: "",
  color: "",
  size: "",
  price: 0,
  quantityTotal: 0,
  description: "",
};

function joinWithAnd(items: string[]): string {
  if (items.length <= 1) return items[0] ?? "";
  if (items.length === 2) return `${items[0]} and ${items[1]}`;
  return `${items.slice(0, -1).join(", ")}, and ${items[items.length - 1]}`;
}

export function InventoryFormModal({ item, onClose, onSaved }: InventoryFormModalProps) {
  const { types, colors, sizes } = useCategories();
  const [currentItem, setCurrentItem] = useState<InventoryItem | undefined>(item);
  const [form, setForm] = useState<InventoryItemCreateInput>(
    item
      ? {
          type: item.type,
          color: item.color ?? "",
          size: item.size ?? "",
          price: item.price,
          quantityTotal: item.quantityTotal,
          description: item.description ?? "",
        }
      : emptyForm,
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useDismissingError();
  const [invalidFields, setInvalidFields] = useState<Set<string>>(new Set());

  // The field highlights exist only to point at the current error, so they clear alongside it
  // when the shared hook's auto-dismiss timer fires.
  useEffect(() => {
    if (!error) setInvalidFields(new Set());
  }, [error]);

  function updateField<K extends keyof InventoryItemCreateInput>(key: K, value: InventoryItemCreateInput[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setInvalidFields(new Set());

    if (!currentItem) {
      const missing: string[] = [];
      if (!form.type) missing.push("type");
      if (!form.color) missing.push("color");
      if (!form.size) missing.push("size");
      if (missing.length > 0) {
        setError(`${joinWithAnd(missing).replace(/^./, (c) => c.toUpperCase())} ${missing.length > 1 ? "are" : "is"} required.`);
        setInvalidFields(new Set(missing));
        return;
      }
    }

    if (form.price <= 0) {
      setError("Price must be greater than 0.");
      setInvalidFields(new Set(["price"]));
      return;
    }

    setSaving(true);
    try {
      const updateInput: InventoryItemUpdateInput = {
        price: form.price,
        quantityTotal: form.quantityTotal,
        description: form.description,
      };
      const saved = currentItem
        ? await updateInventoryItem(currentItem.id, updateInput)
        : await createInventoryItem(form);
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
      <form
        className={`form form--medium-labels inventory-form inventory-form--${currentItem ? "edit" : "add"}`}
        onSubmit={handleSubmit}
        noValidate
      >
        <div className="form-grid">
          <label>
            Type
            {currentItem ? (
              <input type="text" value={form.type} disabled />
            ) : (
              <select
                className={invalidFields.has("type") ? "field-invalid" : undefined}
                value={form.type}
                onChange={(e) => updateField("type", e.target.value)}
              >
                <option value="">Select a type</option>
                {types.map((type) => (
                  <option key={type.id} value={type.name}>
                    {type.name}
                  </option>
                ))}
              </select>
            )}
          </label>
          <label>
            Color
            {currentItem ? (
              <input type="text" value={form.color} disabled />
            ) : (
              <select
                className={invalidFields.has("color") ? "field-invalid" : undefined}
                value={form.color}
                onChange={(e) => updateField("color", e.target.value)}
              >
                <option value="">Select a color</option>
                {colors.map((color) => (
                  <option key={color.id} value={color.name}>
                    {color.name}
                  </option>
                ))}
              </select>
            )}
          </label>
          <label>
            Size
            {currentItem ? (
              <input type="text" value={form.size} disabled />
            ) : (
              <select
                className={invalidFields.has("size") ? "field-invalid" : undefined}
                value={form.size}
                onChange={(e) => updateField("size", e.target.value)}
              >
                <option value="">Select a size</option>
                {sizes.map((size) => (
                  <option key={size.id} value={size.name}>
                    {size.name}
                  </option>
                ))}
              </select>
            )}
          </label>
          <label>
            Price ($)
            <QuantityStepper
              className={invalidFields.has("price") ? "field-invalid" : undefined}
              value={form.price}
              min={0}
              max={Infinity}
              step={1}
              onChange={(value) => updateField("price", value)}
              ariaLabel="Price"
            />
          </label>
          <label>
            {/* The total on hand. Units held by confirmed requests are shown separately in the
                inventory table and subtracted from what customers see. */}
            Total Quantity
            <QuantityStepper
              value={form.quantityTotal}
              min={0}
              max={Infinity}
              onChange={(value) => updateField("quantityTotal", value)}
              ariaLabel="Total quantity"
            />
          </label>
        </div>
        <label>
          Description
          <div className="textarea-wrapper">
            <textarea
              className="textarea-scroll"
              rows={3}
              value={form.description}
              onChange={(e) => updateField("description", e.target.value)}
            />
          </div>
        </label>
        <button type="submit" className="btn btn-primary inventory-form-submit" disabled={saving}>
          {saving ? "Saving..." : currentItem ? "Save Changes" : "Create Item"}
        </button>
        <FormError message={error} prominent />
      </form>

      {currentItem ? (
        <ImageManager item={currentItem} onItemUpdated={(updated) => { setCurrentItem(updated); onSaved(); }} />
      ) : (
        <p className="state-message state-message--compact">Save the item first, then you can add photos.</p>
      )}
    </Modal>
  );
}
