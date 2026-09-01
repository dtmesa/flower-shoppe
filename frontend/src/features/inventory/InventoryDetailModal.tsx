import { useState } from "react";
import { Flower } from "lucide-react";
import type { InventoryItem } from "./types";
import { getCoverImageIndex } from "./imageHelpers";
import { Modal } from "../../components/Modal";
import { QuantityStepper } from "../../components/QuantityStepper";
import { useCart } from "../cart/CartContext";
import { uploadUrl } from "../../lib/apiClient";
import { formatPrice } from "../../lib/format";

interface InventoryDetailModalProps {
  item: InventoryItem;
  onClose: () => void;
}

export function InventoryDetailModal({ item, onClose }: InventoryDetailModalProps) {
  const { addToCart } = useCart();
  const [activeImageIndex, setActiveImageIndex] = useState(() => getCoverImageIndex(item));
  const [quantity, setQuantity] = useState(1);
  const inStock = item.quantityAvailable > 0;
  const activeImage = item.images[activeImageIndex];

  function handleAddToCart() {
    addToCart(item, quantity);
    onClose();
  }

  return (
    // Color leads here for the same reason it leads on the card and the cart line - it's the
    // characteristic customers actually shop by; type/size are the qualifiers.
    <Modal title={item.color ?? item.type} onClose={onClose} wide>
      <div className="detail-layout">
        <div className="detail-gallery">
          <div className="detail-gallery-main">
            {activeImage ? (
              <img src={uploadUrl(activeImage.url)} alt={item.color ?? item.type} />
            ) : (
              <div className="inventory-card-image-placeholder" aria-hidden="true">
                <Flower size={48} strokeWidth={1.5} />
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
                  <img src={uploadUrl(image.url)} alt="" />
                </button>
              ))}
            </div>
          )}
        </div>
        <div className="detail-info">
          <p className="inventory-card-meta">
            {[item.type, item.size].filter(Boolean).join(" · ")}
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
