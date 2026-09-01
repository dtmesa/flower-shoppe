import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useReservations } from "../features/reservations/reservationsApi";

export function AdminLayout() {
  const { reservations } = useReservations();
  const pendingCount = reservations.filter((r) => r.status === "NEW").length;

  // Remounting the badge (via a changing key) replays its CSS mount animation - used here to
  // "hop" on the very first render and again any time the pending count goes up, but not on a
  // plain re-render or when the count drops.
  const previousCountRef = useRef<number | null>(null);
  const [hopKey, setHopKey] = useState(0);
  useEffect(() => {
    if (previousCountRef.current === null || pendingCount > previousCountRef.current) {
      setHopKey((key) => key + 1);
    }
    previousCountRef.current = pendingCount;
  }, [pendingCount]);

  return (
    <div className="page">
      <h1>Admin Dashboard</h1>

      <div className="tabs">
        <NavLink to="/admin/inventory" className={({ isActive }) => `tab${isActive ? " tab--active" : ""}`}>
          <span className="tab-label">Inventory</span>
        </NavLink>
        <NavLink to="/admin/reservations" className={({ isActive }) => `tab${isActive ? " tab--active" : ""}`}>
          <span className="tab-label">Pickup Requests</span>
          {pendingCount > 0 && (
            <span className="tab-badge" key={hopKey}>
              {pendingCount}
            </span>
          )}
        </NavLink>
        <NavLink to="/admin/categories" className={({ isActive }) => `tab${isActive ? " tab--active" : ""}`}>
          <span className="tab-label">Categories</span>
        </NavLink>
        <NavLink to="/admin/account" className={({ isActive }) => `tab${isActive ? " tab--active" : ""}`}>
          <span className="tab-label">Account</span>
        </NavLink>
      </div>

      <Outlet />
    </div>
  );
}
