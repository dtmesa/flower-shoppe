import { NavLink, Outlet } from "react-router-dom";
import { useReservations } from "../features/reservations/reservationsApi";

export function AdminLayout() {
  const { reservations } = useReservations();
  const pendingCount = reservations.filter((r) => r.status === "PENDING").length;

  return (
    <div className="page">
      <h1>Admin Dashboard</h1>

      <div className="tabs">
        <NavLink to="/admin/inventory" className={({ isActive }) => `tab${isActive ? " tab--active" : ""}`}>
          Inventory
        </NavLink>
        <NavLink to="/admin/reservations" className={({ isActive }) => `tab${isActive ? " tab--active" : ""}`}>
          Pickup Requests
          {pendingCount > 0 && <span className="tab-badge">{pendingCount}</span>}
        </NavLink>
      </div>

      <Outlet />
    </div>
  );
}
