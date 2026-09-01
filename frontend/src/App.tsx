import { Navigate, Route, Routes } from "react-router-dom";
import { Header } from "./components/Header";
import { ThemeToggle } from "./components/ThemeToggle";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { CatalogPage } from "./features/inventory/CatalogPage";
import { LoginPage } from "./features/auth/LoginPage";
import { AdminInventoryPage } from "./features/inventory/admin/AdminInventoryPage";
import { AdminCategoriesPage } from "./features/inventory/admin/AdminCategoriesPage";
import { AdminReservationsPage } from "./features/reservations/admin/AdminReservationsPage";
import { AdminAccountPage } from "./features/auth/AdminAccountPage";
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
            <Route path="categories" element={<AdminCategoriesPage />} />
            <Route path="account" element={<AdminAccountPage />} />
          </Route>
        </Routes>
      </main>
      <ThemeToggle />
    </>
  );
}

export default App;
