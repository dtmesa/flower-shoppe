import { useState, type FormEvent } from "react";
import { Modal } from "../../components/Modal";
import { QuantityStepper } from "../../components/QuantityStepper";
import { useCart } from "./CartContext";
import { createReservation } from "../reservations/reservationsApi";
import { extractErrorMessage } from "../../lib/apiClient";

const API_BASE = import.meta.env.VITE_API_BASE_URL;

function formatPrice(price: number): string {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(price);
}

type View = "cart" | "checkout" | "success";

export function CartModal() {
  const { lines, itemCount, totalValue, closeCart, updateQuantity, removeFromCart, clearCart } = useCart();
  const [view, setView] = useState<View>("cart");
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [customerEmail, setCustomerEmail] = useState("");
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submittedCount, setSubmittedCount] = useState(0);
  const [submittedContact, setSubmittedContact] = useState("");

  function handleClose() {
    setView("cart");
    setError(null);
    closeCart();
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    if (!customerPhone.trim() && !customerEmail.trim()) {
      setError("Provide a phone number or email address so we can reach you.");
      return;
    }

    setSubmitting(true);
    try {
      await Promise.all(
        lines.map((line) =>
          createReservation({
            inventoryItemId: line.item.id,
            customerName,
            customerPhone,
            customerEmail,
            quantityRequested: line.quantity,
            notes,
          }),
        ),
      );
      setSubmittedCount(itemCount);
      setSubmittedContact(customerPhone.trim() || customerEmail.trim());
      clearCart();
      setView("success");
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  if (view === "success") {
    return (
      <Modal title="Request sent" onClose={handleClose}>
        <p>
          Thanks, {customerName}! Your pickup request for {submittedCount} {submittedCount === 1 ? "item" : "items"}
          {" "}has been sent. We&apos;ll reach out at {submittedContact} to arrange pickup.
        </p>
        <button type="button" className="btn btn-primary" onClick={handleClose}>
          Done
        </button>
      </Modal>
    );
  }

  if (view === "checkout") {
    return (
      <Modal title="Request for Pickup" onClose={handleClose} wide>
        <form className="form" onSubmit={handleSubmit}>
          {error && <p className="form-error">{error}</p>}
          <label>
            Your name
            <input type="text" required value={customerName} onChange={(e) => setCustomerName(e.target.value)} />
          </label>
          <label>
            Phone
            <input
              type="tel"
              placeholder="Optional if you provide an email"
              value={customerPhone}
              onChange={(e) => setCustomerPhone(e.target.value)}
            />
          </label>
          <label>
            Email
            <input
              type="email"
              placeholder="Optional if you provide a phone number"
              value={customerEmail}
              onChange={(e) => setCustomerEmail(e.target.value)}
            />
          </label>
          <label>
            Notes (optional)
            <textarea
              rows={3}
              placeholder="Preferred pickup day, questions, etc."
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </label>
          <div className="cart-checkout-summary">
            {itemCount} {itemCount === 1 ? "item" : "items"} · {formatPrice(totalValue)}
          </div>
          <div className="cart-checkout-actions">
            <button type="button" className="btn btn-secondary" onClick={() => setView("cart")}>
              Back to cart
            </button>
            <button type="submit" className="btn btn-primary" disabled={submitting}>
              {submitting ? "Sending..." : "Send Request"}
            </button>
          </div>
        </form>
      </Modal>
    );
  }

  return (
    <Modal title="Your Cart" onClose={handleClose} wide>
      {lines.length === 0 ? (
        <p className="state-message">Your cart is empty. Add a plant to get started.</p>
      ) : (
        <>
          <div className="cart-lines">
            {lines.map((line) => {
              const coverImage = line.item.images[0];
              return (
                <div className="cart-line" key={line.item.id}>
                  <div className="cart-line-image">
                    {coverImage ? (
                      <img src={`${API_BASE}${coverImage.url}`} alt={line.item.type} />
                    ) : (
                      <span aria-hidden="true">🌸</span>
                    )}
                  </div>
                  <div className="cart-line-info">
                    <p className="cart-line-title">{line.item.type}</p>
                    <p className="inventory-card-meta">
                      {[line.item.color, line.item.size].filter(Boolean).join(" · ")}
                    </p>
                    <p className="inventory-card-price">{formatPrice(line.item.price)}</p>
                  </div>
                  <div className="cart-line-controls">
                    <QuantityStepper
                      value={line.quantity}
                      min={1}
                      max={line.item.quantityAvailable}
                      onChange={(quantity) => updateQuantity(line.item.id, quantity)}
                      ariaLabel={`Quantity for ${line.item.type}`}
                    />
                    <button
                      type="button"
                      className="link-button"
                      onClick={() => removeFromCart(line.item.id)}
                    >
                      Remove
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
          <div className="cart-checkout-summary">
            Total: {itemCount} {itemCount === 1 ? "item" : "items"} · {formatPrice(totalValue)}
          </div>
          <button type="button" className="btn btn-primary" onClick={() => setView("checkout")}>
            Request for Pickup
          </button>
        </>
      )}
    </Modal>
  );
}
