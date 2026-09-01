import { Flower } from "lucide-react";
import type { InventoryItem } from "./types";
import { getCoverImage } from "./imageHelpers";
import { uploadUrl } from "../../lib/apiClient";
import { formatPrice } from "../../lib/format";

export function InventoryCard({ item, onSelect }: { item: InventoryItem; onSelect: () => void }) {
  const coverImage = getCoverImage(item);
  const inStock = item.quantityAvailable > 0;

  return (
    <button type="button" className="inventory-card" onClick={onSelect}>
      <div className="inventory-card-image">
        {coverImage ? (
          <img src={uploadUrl(coverImage.url)} alt={item.type} loading="lazy" />
        ) : (
          <div className="inventory-card-image-placeholder" aria-hidden="true">
            <Flower size={40} strokeWidth={1.5} />
          </div>
        )}
        <span className={`stock-badge ${inStock ? "stock-badge--in" : "stock-badge--out"}`}>
          {inStock ? `${item.quantityAvailable} available` : "Out of stock"}
        </span>
      </div>
      <div className="inventory-card-body">
        <h3>{item.color}</h3>
        <p className="inventory-card-meta">
          {[item.type, item.size].filter(Boolean).join(" · ")}
        </p>
        <p className="inventory-card-price">{formatPrice(item.price)}</p>
      </div>
    </button>
  );
}
