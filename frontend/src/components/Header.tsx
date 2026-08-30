import type { CSSProperties } from "react";
import { Link, useLocation } from "react-router-dom";
import { useAuth } from "../features/auth/AuthContext";
import { useCart } from "../features/cart/CartContext";
import { CartIcon } from "../features/cart/CartIcon";
import { CartModal } from "../features/cart/CartModal";

const SITE_TITLE = "Flower Shop";

export function Header() {
  const { isAuthenticated, logout } = useAuth();
  const { isOpen } = useCart();
  const location = useLocation();
  const isAdminRoute = location.pathname.startsWith("/admin");

  return (
    <header className="site-header">
      <Link to="/" className="site-brand">
        <span className="site-brand-text">
        {SITE_TITLE.split("").map((char, index) => (
          <span key={index} className="wave-letter" style={{ "--i": index } as CSSProperties}>
            {char === " " ? " " : char}
          </span>
        ))}
        </span>
      </Link>
      <div className="site-header-right">
        {isAuthenticated && (
          <nav className="site-nav">
            <Link to="/admin">Admin</Link>
            <button type="button" className="link-button" onClick={logout}>
              Log out
            </button>
          </nav>
        )}
        {!isAdminRoute && <CartIcon />}
      </div>
      {isOpen && !isAdminRoute && <CartModal />}
    </header>
  );
}
