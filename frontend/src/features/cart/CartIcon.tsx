import { ShoppingCart } from "lucide-react";
import { useCart } from "./CartContext";
import { formatPrice } from "../../lib/format";

export function CartIcon() {
  const { itemCount, totalValue, openCart } = useCart();

  return (
    <button type="button" className="cart-trigger" onClick={openCart} aria-label="Open cart">
      <span className="cart-summary">{formatPrice(totalValue)}</span>
      <span className="cart-icon">
        <ShoppingCart size={26} strokeWidth={2} aria-hidden="true" />
        {itemCount > 0 && <span className="cart-badge">{itemCount}</span>}
      </span>
    </button>
  );
}
