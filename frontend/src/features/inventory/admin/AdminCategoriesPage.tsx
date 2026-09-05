import { useState, type FormEvent } from "react";
import { Check, CirclePlus, CircleX, SquarePen, X } from "lucide-react";
import type { Category, CategoryKind } from "../categoriesApi";
import { createCategory, deleteCategory, updateCategory, useCategories } from "../categoriesApi";
import { FormError, useDismissingError } from "../../../components/FormError";
import { useConfirm } from "../../../components/ConfirmDialog";
import { extractErrorMessage } from "../../../lib/apiClient";

const KIND_LABELS: Record<CategoryKind, string> = {
  TYPE: "Type",
  COLOR: "Color",
  SIZE: "Size",
};

interface CategorySectionProps {
  kind: CategoryKind;
  categories: Category[];
  onChanged: () => Promise<unknown>;
}

function CategorySection({ kind, categories, onChanged }: CategorySectionProps) {
  const [newName, setNewName] = useState("");
  const [newCode, setNewCode] = useState("");
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editName, setEditName] = useState("");
  const [editCode, setEditCode] = useState("");
  const [error, setError] = useDismissingError();
  const [saving, setSaving] = useState(false);
  const [confirm, confirmDialog] = useConfirm();

  // Two different categories of the same kind sharing a code makes their generated item IDs
  // collide (see InventoryService.GenerateIdAsync) - not blocked outright, just flagged so an
  // admin can back out before it causes confusing duplicate IDs down the line.
  function findCodeConflict(code: string, excludeId?: number): Category | undefined {
    const normalized = code.trim().toUpperCase();
    return categories.find((category) => category.id !== excludeId && category.code.toUpperCase() === normalized);
  }

  /** Returns true if there's no conflict, or the admin chose to proceed despite one. */
  async function confirmCodeConflict(code: string, action: string, excludeId?: number): Promise<boolean> {
    const conflict = findCodeConflict(code, excludeId);
    if (!conflict) return true;
    return confirm({
      title: "Duplicate Code",
      message: `The code "${code.trim()}" is already used by "${conflict.name}". Two ${KIND_LABELS[kind].toLowerCase()} entries sharing a code will produce identical item ID tags.`,
      confirmLabel: action,
      centered: true,
    });
  }

  async function handleAdd(event: FormEvent) {
    event.preventDefault();
    setError(null);

    if (!newName.trim() || !newCode.trim()) {
      setError("Name and code are required");
      return;
    }

    if (!(await confirmCodeConflict(newCode, "Create Anyway"))) return;

    setSaving(true);
    try {
      await createCategory(kind, newName.trim(), newCode.trim());
      setNewName("");
      setNewCode("");
      await onChanged();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  function startEdit(category: Category) {
    setEditingId(category.id);
    setEditName(category.name);
    setEditCode(category.code);
  }

  async function handleSaveEdit(id: number) {
    setError(null);

    if (!(await confirmCodeConflict(editCode, "Save Anyway", id))) return;

    setSaving(true);
    try {
      await updateCategory(id, editName.trim(), editCode.trim());
      setEditingId(null);
      await onChanged();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(category: Category) {
    setError(null);

    const proceed = await confirm({
      title: `Delete ${KIND_LABELS[kind]}`,
      message: `Delete "${category.name}"? This cannot be undone.`,
      confirmLabel: "Delete",
      danger: true,
      centered: true,
    });
    if (!proceed) return;

    setSaving(true);
    try {
      await deleteCategory(category.id);
      await onChanged();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="category-section">
      <h3>{KIND_LABELS[kind]}</h3>
      <div className="table-wrapper">
        <div className="table-scroll">
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Code</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {categories.map((category) => (
              <tr key={category.id}>
                {editingId === category.id ? (
                  <>
                    <td>
                      <input type="text" value={editName} onChange={(e) => setEditName(e.target.value)} />
                    </td>
                    <td>
                      <input
                        type="text"
                        maxLength={4}
                        value={editCode}
                        onChange={(e) => setEditCode(e.target.value)}
                      />
                    </td>
                    <td>
                      <div className="table-actions">
                        <button
                          type="button"
                          className="row-icon-btn"
                          disabled={saving}
                          onClick={() => handleSaveEdit(category.id)}
                          aria-label="Save changes"
                          title="Save changes"
                        >
                          <Check size={22} strokeWidth={2} aria-hidden="true" />
                        </button>
                        <button
                          type="button"
                          className="row-icon-btn"
                          onClick={() => setEditingId(null)}
                          aria-label="Cancel"
                          title="Cancel"
                        >
                          <X size={22} strokeWidth={2} aria-hidden="true" />
                        </button>
                      </div>
                    </td>
                  </>
                ) : (
                  <>
                    <td>{category.name}</td>
                    <td>{category.code}</td>
                    <td>
                      <div className="table-actions">
                        <button
                          type="button"
                          className="row-icon-btn"
                          onClick={() => startEdit(category)}
                          aria-label="Edit category"
                          title="Edit category"
                        >
                          <SquarePen size={22} strokeWidth={2} aria-hidden="true" />
                        </button>
                        <button
                          type="button"
                          className="row-icon-btn"
                          disabled={saving}
                          onClick={() => handleDelete(category)}
                          aria-label="Delete category"
                          title="Delete category"
                        >
                          <CircleX size={22} strokeWidth={2} aria-hidden="true" />
                        </button>
                      </div>
                    </td>
                  </>
                )}
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      </div>
      <form className="category-add-form" onSubmit={handleAdd} noValidate>
        <input
          type="text"
          placeholder={`New ${KIND_LABELS[kind].toLowerCase()} name`}
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
        />
        <input
          type="text"
          placeholder="Code"
          maxLength={4}
          value={newCode}
          onChange={(e) => setNewCode(e.target.value)}
        />
        <button
          type="submit"
          className="row-icon-btn category-add-btn"
          disabled={saving}
          aria-label={`Add ${KIND_LABELS[kind].toLowerCase()}`}
          title={`Add ${KIND_LABELS[kind].toLowerCase()}`}
        >
          <CirclePlus size={28} strokeWidth={2} aria-hidden="true" />
        </button>
      </form>
      <FormError message={error} prominent />
      {confirmDialog}
    </div>
  );
}

export function AdminCategoriesPage() {
  const { types, colors, sizes, error, isLoading, refresh } = useCategories();

  return (
    <div>
      <p className="state-message state-message--intro">
        These are the type, color, &amp; size options customers filter by and you choose from when
        adding inventory. Each category's code feeds directly into new items' auto-generated ID tags.
      </p>
      {error && <p className="state-message state-message--error">{extractErrorMessage(error)}</p>}
      {!isLoading && (
        <div className="category-sections">
          <CategorySection kind="TYPE" categories={types} onChanged={() => refresh()} />
          <CategorySection kind="COLOR" categories={colors} onChanged={() => refresh()} />
          <CategorySection kind="SIZE" categories={sizes} onChanged={() => refresh()} />
        </div>
      )}
    </div>
  );
}
