import type { InventoryItem } from "./types";

const API_BASE = import.meta.env.VITE_API_BASE_URL;

function formatPrice(price: number): string {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(price);
}

export function InventoryCard({ item, onSelect }: { item: InventoryItem; onSelect: () => void }) {
  const coverImage = item.images[0];
  const inStock = item.quantityAvailable > 0;

  return (
    <button type="button" className="inventory-card" onClick={onSelect}>
      <div className="inventory-card-image">
        {coverImage ? (
          <img src={`${API_BASE}${coverImage.url}`} alt={item.type} loading="lazy" />
        ) : (
          <div className="inventory-card-image-placeholder" aria-hidden="true">
            🌸
          </div>
        )}
        <span className={`stock-badge ${inStock ? "stock-badge--in" : "stock-badge--out"}`}>
          {inStock ? `${item.quantityAvailable} available` : "Out of stock"}
        </span>
      </div>
      <div className="inventory-card-body">
        <h3>{item.type}</h3>
        <p className="inventory-card-meta">
          {[item.color, item.size].filter(Boolean).join(" · ")}
        </p>
        <p className="inventory-card-price">{formatPrice(item.price)}</p>
      </div>
    </button>
  );
}
