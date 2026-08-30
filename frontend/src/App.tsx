import { Navigate, Route, Routes } from "react-router-dom";
import { Header } from "./components/Header";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { CatalogPage } from "./features/inventory/CatalogPage";
import { LoginPage } from "./features/auth/LoginPage";
import { AdminInventoryPage } from "./features/inventory/admin/AdminInventoryPage";
import { AdminReservationsPage } from "./features/reservations/admin/AdminReservationsPage";
import { AdminLayout } from "./pages/AdminLayout";

function App() {
  return (
    <>
      <Header />
      <main>
        <Routes>
          <Route path="/" element={<CatalogPage />} />
          <Route path="/admin/login" element={<LoginPage />} />
          <Route
            path="/admin"
            element={
              <ProtectedRoute>
                <AdminLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<Navigate to="inventory" replace />} />
            <Route path="inventory" element={<AdminInventoryPage />} />
            <Route path="reservations" element={<AdminReservationsPage />} />
          </Route>
        </Routes>
      </main>
    </>
  );
}

export default App;
