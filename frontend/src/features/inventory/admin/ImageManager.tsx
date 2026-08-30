import { useRef, useState } from "react";
import type { InventoryItem } from "../types";
import { deleteInventoryImage, uploadInventoryImage } from "../inventoryApi";
import { extractErrorMessage } from "../../../lib/apiClient";

const API_BASE = import.meta.env.VITE_API_BASE_URL;

interface ImageManagerProps {
  item: InventoryItem;
  onItemUpdated: (item: InventoryItem) => void;
}

export function ImageManager({ item, onItemUpdated }: ImageManagerProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function handleFileChange(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    setError(null);
    setUploading(true);
    try {
      const updated = await uploadInventoryImage(item.id, file);
      onItemUpdated(updated);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  async function handleDelete(imageId: number) {
    setError(null);
    setDeletingId(imageId);
    try {
      const updated = await deleteInventoryImage(item.id, imageId);
      onItemUpdated(updated);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div className="image-manager">
      <h3>Photos</h3>
      {error && <p className="form-error">{error}</p>}
      <div className="image-manager-grid">
        {item.images.map((image) => (
          <div className="image-manager-thumb" key={image.id}>
            <img src={`${API_BASE}${image.url}`} alt="" />
            <button
              type="button"
              className="btn btn-danger btn-small"
              onClick={() => handleDelete(image.id)}
              disabled={deletingId === image.id}
            >
              {deletingId === image.id ? "Removing..." : "Remove"}
            </button>
          </div>
        ))}
      </div>
      <label className="btn btn-secondary btn-upload">
        {uploading ? "Uploading..." : "Add Photo"}
        <input
          type="file"
          ref={fileInputRef}
          accept="image/jpeg,image/png,image/webp,image/gif"
          onChange={handleFileChange}
          disabled={uploading}
          hidden
        />
      </label>
    </div>
  );
}
