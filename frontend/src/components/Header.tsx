import type { CSSProperties } from "react";
import { Link, useLocation } from "react-router-dom";
import { LogOut } from "lucide-react";
import { useAuth } from "../features/auth/AuthContext";
import { useCart } from "../features/cart/CartContext";
import { CartIcon } from "../features/cart/CartIcon";
import { CartModal } from "../features/cart/CartModal";

const SITE_TITLE = "Flower Shoppe";

function scrollToContact() {
  document.getElementById("contact")?.scrollIntoView({ behavior: "smooth" });

  // Delayed so the wave starts once the smooth scroll has actually brought the section into
  // view, instead of playing off-screen while the page is still scrolling toward it.
  window.setTimeout(() => {
    const title = document.querySelector(".contact-footer-title");
    if (!title) return;
    // Remove-then-reflow-then-add so repeat clicks restart the animation instead of no-op'ing
    // because the class (and thus the animation-name) never actually changed.
    title.classList.remove("contact-footer-title--wave");
    void (title as HTMLElement).offsetWidth;
    title.classList.add("contact-footer-title--wave");
    window.setTimeout(() => title.classList.remove("contact-footer-title--wave"), 1500);
  }, 500);
}

export function Header() {
  const { isAuthenticated, logout } = useAuth();
  const { isOpen } = useCart();
  const location = useLocation();
  const isAdminRoute = location.pathname.startsWith("/admin");

  return (
    <header className="site-header">
      <div className="site-brand-group">
      <Link to="/" className="site-brand">
        <span className="site-brand-text">
        {SITE_TITLE.split("").map((char, index) => (
          <span key={index} className="wave-letter" style={{ "--i": index } as CSSProperties}>
            {char === " " ? " " : char}
          </span>
        ))}
        </span>
      </Link>
      {!isAdminRoute && (
        <button type="button" className="site-subheader-link" onClick={scrollToContact}>
          Contact Us
        </button>
      )}
      </div>
      <div className="site-header-right">
        {isAuthenticated && (
          <nav className="site-nav">
            <button type="button" className="logout-button" onClick={logout} aria-label="Log out">
              <LogOut size={20} strokeWidth={2} aria-hidden="true" />
            </button>
          </nav>
        )}
        {!isAdminRoute && <CartIcon />}
      </div>
      {isOpen && !isAdminRoute && <CartModal />}
    </header>
  );
}
