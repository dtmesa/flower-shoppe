import { useRef, useState, type FormEvent } from "react";
import { Flower } from "lucide-react";
import { Modal } from "../../components/Modal";
import { FormError, useDismissingError } from "../../components/FormError";
import { QuantityStepper } from "../../components/QuantityStepper";
import { useCart } from "./CartContext";
import { getCoverImage } from "../inventory/imageHelpers";
import { createPickupRequest } from "../reservations/reservationsApi";
import { extractErrorMessage, uploadUrl } from "../../lib/apiClient";
import { formatPrice } from "../../lib/format";
import { formatPhoneInput, isCompletePhone } from "../../lib/phone";
import { shakeFields } from "../../lib/shake";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

type View = "cart" | "checkout" | "success";

export function CartModal() {
  const { lines, itemCount, totalValue, closeCart, updateQuantity, removeFromCart, clearCart } = useCart();
  const [view, setView] = useState<View>("cart");
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [customerEmail, setCustomerEmail] = useState("");
  const [notes, setNotes] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useDismissingError();
  const [submittedCount, setSubmittedCount] = useState(0);
  const [submittedContact, setSubmittedContact] = useState("");
  const nameInputRef = useRef<HTMLInputElement>(null);
  const phoneInputRef = useRef<HTMLInputElement>(null);
  const emailInputRef = useRef<HTMLInputElement>(null);

  function handleClose() {
    setView("cart");
    setError(null);
    closeCart();
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    if (!customerName.trim()) {
      setError("Enter your name.");
      shakeFields(nameInputRef.current);
      return;
    }

    if (!customerPhone.trim() && !customerEmail.trim()) {
      setError("Provide a phone number or email address so we can reach you.");
      shakeFields(phoneInputRef.current, emailInputRef.current);
      return;
    }

    if (customerPhone.trim() && !isCompletePhone(customerPhone)) {
      setError("Enter a 10-digit phone number, e.g. (555) 123-4567.");
      shakeFields(phoneInputRef.current);
      return;
    }

    if (customerEmail.trim() && !EMAIL_PATTERN.test(customerEmail.trim())) {
      setError("Enter a valid email address.");
      shakeFields(emailInputRef.current);
      return;
    }

    setSubmitting(true);
    try {
      await createPickupRequest({
        customerName,
        customerPhone,
        customerEmail,
        notes,
        items: lines.map((line) => ({
          inventoryItemId: line.item.id,
          quantityRequested: line.quantity,
        })),
      });
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
      <Modal title="Request Sent" onClose={handleClose} centeredTitle>
        <p className="detail-description detail-description--centered">
          Your pickup request for {submittedCount} {submittedCount === 1 ? "item" : "items"}
          {" "}has been sent. We&apos;ll reach out at {submittedContact} to arrange a pickup.
        </p>
        <div className="cart-checkout-actions cart-checkout-actions--centered">
          <button type="button" className="btn btn-primary" onClick={handleClose}>
            Done
          </button>
        </div>
      </Modal>
    );
  }

  if (view === "checkout") {
    return (
      <Modal title="Request for Pickup" onClose={handleClose} wide>
        <form className="form" onSubmit={handleSubmit} noValidate>
          <label>
            Your Name
            <input
              type="text"
              ref={nameInputRef}
              value={customerName}
              onChange={(e) => setCustomerName(e.target.value)}
            />
          </label>
          <label>
            Phone
            <input
              type="tel"
              ref={phoneInputRef}
              placeholder="Provide either a phone number or email address"
              value={customerPhone}
              onChange={(e) => setCustomerPhone(formatPhoneInput(e.target.value))}
            />
          </label>
          <label>
            Email
            <input
              type="email"
              ref={emailInputRef}
              placeholder="Provide either a phone number or email address"
              value={customerEmail}
              onChange={(e) => setCustomerEmail(e.target.value)}
            />
          </label>
          <label>
            Notes
            <textarea
              rows={3}
              placeholder="Optional: Preferred pickup day, questions, etc."
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </label>
          <FormError message={error} reserveSpace tight />
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
              const coverImage = getCoverImage(line.item);
              return (
                <div className="cart-line" key={line.item.id}>
                  <div className="cart-line-image">
                    {coverImage ? (
                      <img src={uploadUrl(coverImage.url)} alt={line.item.color ?? line.item.type} />
                    ) : (
                      <Flower size={28} strokeWidth={1.5} aria-hidden="true" />
                    )}
                  </div>
                  <div className="cart-line-info">
                    <p className="cart-line-title">{line.item.color}</p>
                    <p className="inventory-card-meta">
                      {[line.item.type, line.item.size].filter(Boolean).join(" · ")}
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
