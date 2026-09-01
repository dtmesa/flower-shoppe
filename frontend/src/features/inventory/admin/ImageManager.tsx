import { useRef, useState } from "react";
import { Flower, Plus, X } from "lucide-react";
import type { InventoryItem } from "../types";
import { deleteInventoryImage, setPrimaryInventoryImage, uploadInventoryImage } from "../inventoryApi";
import { FormError, useDismissingError } from "../../../components/FormError";
import { extractErrorMessage, uploadUrl } from "../../../lib/apiClient";

interface ImageManagerProps {
  item: InventoryItem;
  onItemUpdated: (item: InventoryItem) => void;
}

export function ImageManager({ item, onItemUpdated }: ImageManagerProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);
  const [settingPrimaryId, setSettingPrimaryId] = useState<number | null>(null);
  const [error, setError] = useDismissingError();

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

  async function handleSetPrimary(imageId: number) {
    setError(null);
    setSettingPrimaryId(imageId);
    try {
      const updated = await setPrimaryInventoryImage(item.id, imageId);
      onItemUpdated(updated);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSettingPrimaryId(null);
    }
  }

  return (
    <div className="image-manager">
      <div className="image-manager-header">
        <h3>Photos</h3>
        <button
          type="button"
          className="btn-icon-circle btn-icon-circle--small"
          onClick={() => fileInputRef.current?.click()}
          disabled={uploading}
          aria-label="Add Photo"
          title="Add Photo"
        >
          <Plus size={14} strokeWidth={2.5} aria-hidden="true" />
        </button>
        <input
          type="file"
          ref={fileInputRef}
          accept="image/jpeg,image/png,image/webp,image/gif"
          onChange={handleFileChange}
          disabled={uploading}
          hidden
        />
      </div>
      <FormError message={error} />
      <div className="image-manager-grid">
        {item.images.map((image) => {
          const isEffectivePrimary = image.isPrimary || item.images.length === 1;
          return (
          <div className={`image-manager-thumb${isEffectivePrimary ? " image-manager-thumb--primary" : ""}`} key={image.id}>
            {isEffectivePrimary ? (
              <div className="image-manager-thumb-image">
                <img src={uploadUrl(image.url)} alt="" />
                <span className="image-manager-primary-badge" title="Current thumbnail">
                  <Flower size={15} fill="currentColor" strokeWidth={0} />
                </span>
              </div>
            ) : (
              <button
                type="button"
                className="image-manager-thumb-image image-manager-thumb-button"
                onClick={() => handleSetPrimary(image.id)}
                disabled={settingPrimaryId === image.id}
                title="Set as thumbnail"
              >
                <img src={uploadUrl(image.url)} alt="" />
              </button>
            )}
            <button
              type="button"
              className="image-manager-remove-btn image-manager-remove-btn--corner"
              onClick={() => handleDelete(image.id)}
              disabled={deletingId === image.id}
              aria-label="Remove photo"
              title="Remove photo"
            >
              <X size={16} strokeWidth={2.5} aria-hidden="true" />
            </button>
          </div>
          );
        })}
      </div>
    </div>
  );
}
