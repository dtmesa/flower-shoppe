import { useState } from "react";
import type { InventoryItem } from "./types";
import { Modal } from "../../components/Modal";
import { QuantityStepper } from "../../components/QuantityStepper";
import { useCart } from "../cart/CartContext";

const API_BASE = import.meta.env.VITE_API_BASE_URL;

function formatPrice(price: number): string {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(price);
}

interface InventoryDetailModalProps {
  item: InventoryItem;
  onClose: () => void;
}

export function InventoryDetailModal({ item, onClose }: InventoryDetailModalProps) {
  const { addToCart } = useCart();
  const [activeImageIndex, setActiveImageIndex] = useState(0);
  const [quantity, setQuantity] = useState(1);
  const inStock = item.quantityAvailable > 0;
  const activeImage = item.images[activeImageIndex];

  function handleAddToCart() {
    addToCart(item, quantity);
    onClose();
  }

  return (
    <Modal title={item.type} onClose={onClose} wide>
      <div className="detail-layout">
        <div className="detail-gallery">
          <div className="detail-gallery-main">
            {activeImage ? (
              <img src={`${API_BASE}${activeImage.url}`} alt={item.type} />
            ) : (
              <div className="inventory-card-image-placeholder" aria-hidden="true">
                🌸
              </div>
            )}
          </div>
          {item.images.length > 1 && (
            <div className="detail-gallery-thumbs">
              {item.images.map((image, index) => (
                <button
                  type="button"
                  key={image.id}
                  className={`detail-gallery-thumb${index === activeImageIndex ? " detail-gallery-thumb--active" : ""}`}
                  onClick={() => setActiveImageIndex(index)}
                >
                  <img src={`${API_BASE}${image.url}`} alt="" />
                </button>
              ))}
            </div>
          )}
        </div>
        <div className="detail-info">
          <p className="inventory-card-meta">
            {[item.color, item.size].filter(Boolean).join(" · ")}
          </p>
          <p className="detail-price">{formatPrice(item.price)}</p>
          <p className={`stock-badge ${inStock ? "stock-badge--in" : "stock-badge--out"}`}>
            {inStock ? `${item.quantityAvailable} available for pickup` : "Out of stock"}
          </p>
          {item.description && <p className="detail-description">{item.description}</p>}
          {inStock && (
            <div className="detail-add-to-cart">
              <QuantityStepper
                value={quantity}
                min={1}
                max={item.quantityAvailable}
                onChange={setQuantity}
                ariaLabel="Quantity"
              />
              <button type="button" className="btn btn-primary" onClick={handleAddToCart}>
                Add to Cart
              </button>
            </div>
          )}
        </div>
      </div>
    </Modal>
  );
}
